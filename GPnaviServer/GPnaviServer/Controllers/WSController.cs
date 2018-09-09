using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using GPnaviServer.Models;
using System.IO;
using CsvHelper;
using Microsoft.AspNetCore.Http;
using GPnaviServer.Dtos;
using GPnaviServer.Services;
using AutoMapper;
using System.Text.RegularExpressions;
using GPnaviServer.WebSockets.APIs;

namespace GPnaviServer.Controllers
{
    [Route("[controller]")]
    public class WSController : Controller
    {
        private IUserService _userService;
        private IUserStatusService _userStatusService;
        private IWorkScheduleVersionService _wsvService;
        private IWorkScheduleService _wsmService;
        private IMapper _mapper;


        public WSController(
            IUserService userService,
            IUserStatusService userStatusService,
            IWorkScheduleVersionService workScheduleVersionService, 
            IWorkScheduleService wsmService, 
            IMapper mapper)
        {
            _userService = userService;
            _userStatusService = userStatusService;
            _wsvService = workScheduleVersionService;
            _wsmService = wsmService;
            _mapper = mapper;
        }

        /// <summary>
        /// 入力チェック
        /// </summary>
        /// <param name="loginId">ログイン者ID</param>
        /// <param name="sessionKey">ログイン者のセッションキー</param>
        /// <returns>バリデーションエラーの場合は真</returns>
        private (bool result, UserStatus status) IsInvalidSession(string loginId, string sessionKey)
        {
            ViewBag.LoginName = "";
            if (!string.IsNullOrEmpty(loginId) && !string.IsNullOrEmpty(loginId))
            {
                var status = _userStatusService.GetById(loginId);
                if (status != null && string.Equals(sessionKey, status.SessionKey))
                {
                    ViewBag.LoginName = _userService.GetById(loginId).LoginName;
                    return (true, status);
                }
            }   

            return (false, null);
        }

        [HttpGet("TimeAggregate")]
        public IActionResult TimeAggregate(string loginId, string sessionKey)
        {
            var (result, status) = IsInvalidSession(loginId,  sessionKey);
            if (result)
            {
                return View("TimeAggregate");
            }

            return View("~/Views/Users/Login.cshtml");
        }

        [HttpGet("DayAggregate")]
        public IActionResult DayAggregate(string loginId, string sessionKey)
        {
            var (result, status) = IsInvalidSession(loginId, sessionKey);
            if (result)
            {
                return View("DayAggregate");
            }

            return View("~/Views/Users/Login.cshtml");
        }

        [HttpGet("upload")]
        public IActionResult Upload(string loginId,string sessionKey)
        {
            var (result, status) = IsInvalidSession(loginId, sessionKey);
            if (result)
            {
                return View("upload", status);
            }

            return View("~/Views/Users/Login.cshtml");
        }


        [HttpPost("uploadws")]
        public async Task<IActionResult> UploadWS(IFormFile file,string LoginId,string SessionKey)
        {

            var (result, userStatus) = IsInvalidSession(LoginId, SessionKey);
            if (!result)
            {
                return View("~/Views/Users/Login.cshtml");
            }

            if (file.Length < 1)
            {
                return View("upload", userStatus);
            }

            try { 
                var config = new CsvHelper.Configuration.Configuration
                {
                    HasHeaderRecord = false,
                    MissingFieldFound = null,
                    IgnoreBlankLines = true,
                };

                using (var streamReader = new StreamReader(file.OpenReadStream()))
                using (var csv = new CsvReader(streamReader, config))
                {
                    IEnumerable<WSCsvRow> wsCsvRow = csv.GetRecords<WSCsvRow>();

                    List<WorkScheduleMaster> wsmList;

                    if (csvValidationWsErr(wsCsvRow, out wsmList))
                    {
                        return View("upload", userStatus);
                    }

                    //登録済みの最新のWSマスタバージョンを検索し、存在する場合は有効期限日付時刻を現在時刻で更新する。WSマスタバージョンを追加してバージョン番号を取得する。
                    long latestVer = _wsvService.Add();

                    //DB WSマスタ追加
                    wsmList.ForEach(wsm => wsm.Version = latestVer);

                    int countAdd = _wsmService.Add(wsmList);

                    ViewBag.Message = String.Format(ApiConstant.INFO_UPLOAD_WS_01, countAdd);

                }
            }
            catch (Exception e)
            {
                ViewBag.Message = String.Format(ApiConstant.ERR90);
            }


            return View("upload", userStatus);
        }


        private bool csvValidationWsErr(IEnumerable<WSCsvRow> wsCsvRow, out List<WorkScheduleMaster> wsmList)
        {
            wsmList = new List<WorkScheduleMaster>();

            //スタート時刻のフォーマット
            Regex checktime = new Regex(@"^([0-9]|0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$");

            //時間としての分のフォーマット
            Regex checkMinute = new Regex(@"^[0-9]*[1-9][0-9]*$");

            var prioritySet = new HashSet<string>(){ "H", "M", "L" };
            var iconIdSet = new HashSet<string>()
            {
                "0000",
                "0001",
                "0002",
                "0003",
                "0004",
                "0005",
                "0006",
                "0007",
                "0008",
                "0009",
                "0010",
                "0011"
            };
            var holidaySet = new HashSet<string>() {ApiConstant.HOLIDAY_FALSE, ApiConstant.HOLIDAY_TRUE};

            int line = 0;
            foreach (var ws in wsCsvRow)
            {
                ++line;

                //入力値のチェック
                if (string.IsNullOrWhiteSpace(ws.Start))
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR10, line, ApiConstant.START_JP);
                    return true;
                }
                if (!checktime.IsMatch(ws.Start))
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR11, line, ApiConstant.START_JP);
                    return true;
                }
                if (ws.Start.Length== ApiConstant.START_LENGTH_MIN)
                {
                    ws.Start = "0" + ws.Start;
                }

                if (string.IsNullOrWhiteSpace(ws.Name))
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR10, line, ApiConstant.WS_NAME_JP);
                    return true;
                }
                if ( ws.Name.Length > ApiConstant.WS_NAME_LENGTH_MAX)
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR12, line, ApiConstant.WS_NAME_JP, ApiConstant.WS_NAME_LENGTH_MAX);
                    return true;
                }


                if (string.IsNullOrWhiteSpace(ws.ShortName) )
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR10, line, ApiConstant.WS_SHORTNAME_JP);
                    return true;
                }
                if (ws.ShortName.Length > ApiConstant.WS_SHORTNAME_LENGTH_MAX)
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR12, line, ApiConstant.WS_SHORTNAME_JP, ApiConstant.WS_SHORTNAME_LENGTH_MAX);
                    return true;
                }


                if (string.IsNullOrWhiteSpace(ws.Priority))
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR10, line, ApiConstant.PRIORITY_JP);
                    return true;
                }
                if ( !prioritySet.Contains(ws.Priority))
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR13, line, ApiConstant.PRIORITY_JP);
                    return true;
                }

                if (string.IsNullOrWhiteSpace(ws.IconId))
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR10, line, ApiConstant.ICONID_JP);
                    return true;
                }
                if (!iconIdSet.Contains(ws.IconId))
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR13, line, ApiConstant.ICONID_JP);
                    return true;
                }

                if (string.IsNullOrWhiteSpace(ws.Time))
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR10, line, ApiConstant.WORK_TIME_JP);
                    return true;
                }
                if ( !checkMinute.IsMatch(ws.Time))
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR13, line, ApiConstant.WORK_TIME_JP);
                    return true;
                }

                if (string.IsNullOrWhiteSpace(ws.Holiday) )
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR10, line, ApiConstant.HOLIDAY_JP);
                    return true;
                }
                if ( !holidaySet.Contains(ws.Holiday))
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR13, line, ApiConstant.HOLIDAY_JP);
                    return true;
                }

                var wsm = _mapper.Map<WorkScheduleMaster>(ws);
                wsm.Row = line;
                wsmList.Add(wsm);
            }

            return false;
        }

    }

}