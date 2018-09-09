using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GPnaviServer.Services;
using GPnaviServer.Dtos;
using AutoMapper;
using GPnaviServer.Models;
using Microsoft.AspNetCore.Http;
using GPnaviServer.WebSockets.APIs;
using System.Threading.Tasks;
using System.IO;
using CsvHelper;
using System.Text.RegularExpressions;
using System;

namespace GPnaviServer.Controllers
{
   
   
    public class UsersController : Controller
    {
        private IUserService _userService;
        private IUserVersionService _userVersionService;
        private IUserStatusService _userStatusService;
        private IMapper _mapper;

        public UsersController(
            IUserService userService,
            IUserVersionService userVersionService,
            IUserStatusService userStatusService,
            IMapper mapper)
        {
            _userService = userService;
            _userStatusService = userStatusService;
            _userVersionService = userVersionService;
            _mapper = mapper;
        }


        public IActionResult Login()
        {
            return View();
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

        private (bool result, UserStatus status) IsInvalidSession(UserDto userDto)
        {
            ViewBag.LoginName = "";
            if (userDto != null && !string.IsNullOrEmpty(userDto.LoginId) && !string.IsNullOrEmpty(userDto.SessionKey))
            {
                var status = _userStatusService.GetById(userDto.LoginId);
                if (status != null && string.Equals(userDto.SessionKey, status.SessionKey))
                {
                    ViewBag.LoginName = _userService.GetById(userDto.LoginId).LoginName;
                    return (true, status);
                }
            }

            return (false, null);
        }

        public IActionResult Home(UserDto userDto)
        {
            try
            {
                var (result, status) = IsInvalidSession(userDto);
                if (result)
                {
                    return View("Home", status);
                }
            }
            catch (Exception e)
            {
                ViewBag.Message = String.Format(ApiConstant.ERR90);
                return View("Login");
            }

            return LoginCheck(userDto);
        }

        public IActionResult LoginCheck(UserDto userDto)
        {
            if (userDto==null || string.IsNullOrEmpty(userDto.LoginId))
            {
                userDto.Password = "";
                return View("Login"); 
            }

            try
            {
                var userMaster = _mapper.Map<UserMaster>(userDto);

                //パースワードチェック
                var user = _userService.Authenticate(userDto.LoginId, userDto.Password);

                if (user == null)
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR01);
                    userMaster.Password = "";
                    return View("Login", userMaster);
                }

                //役割チェック　 ROLE_ADMIN または ROLE_WORK
                if (!string.Equals(user.Role, ApiConstant.ROLE_ADMIN))
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR08);
                    userMaster.Password = "";
                    return View("Login", userMaster);
                }


                // 担当者状態のセッションキーチェック
                var userStatus = _userStatusService.GetById(user.LoginId);
                if (string.Equals(userDto.SessionKey, "undefined") || userStatus == null || !string.Equals(userDto.SessionKey, userStatus.SessionKey))
                {
                    string sessionKey = HttpContext.Session.Id;
                    HttpContext.Session.SetString("SessionKey", sessionKey);
                    userStatus = _userStatusService.UpdateOrCreate(user.LoginId, sessionKey);
                }
                ViewBag.LoginName = user.LoginName;

                return View("Home", userStatus);
            }
            catch (Exception e)
            {
                ViewBag.Message = String.Format(ApiConstant.ERR90);
                return View("Login");
            }
        }


        public IActionResult Logout(UserDto userDto)
        {
            if (userDto==null || userDto.LoginId ==null || userDto.SessionKey==null)
            {
                return View("Login");
            }

            try
            {
                _userStatusService.ClearSessionKey(userDto.LoginId, userDto.SessionKey);
            }
            catch (Exception e)
            {
                ViewBag.Message = String.Format(ApiConstant.ERR90);
            }

            return View("Login");
        }

        [HttpPost("uploaduserdata")]
        public async Task<IActionResult> uploadUserData(IFormFile file, string LoginId, string SessionKey)
        {
            var (result, userStatus) = IsInvalidSession(LoginId, SessionKey);
            if (!result)
            {
                return View("~/Views/Users/Login.cshtml");
            }

            if (file.Length<1)
            {
                return View("~/Views/WS/Upload.cshtml", userStatus);
            }

            try
            {
                var config = new CsvHelper.Configuration.Configuration
                {
                    HasHeaderRecord = false,
                    MissingFieldFound = null,
                    IgnoreBlankLines = true,
                };
                using (var streamReader = new StreamReader(file.OpenReadStream()))
                using (var csv = new CsvReader(streamReader, config))
                {
                    IEnumerable<UserCsvRow> userCsvRow = csv.GetRecords<UserCsvRow>();

                    List<UserMaster> userList;

                    if (csvValidationUserErr(userCsvRow, out userList))
                    {
                        return View("~/Views/WS/Upload.cshtml", userStatus);
                    }

                    _userVersionService.Add();

                    int countAdd = _userService.Upload(userList);

                    ViewBag.Message = String.Format(ApiConstant.INFO_UPLOAD_USER_01, countAdd);

                }

            }
            catch (Exception e)
            {
                ViewBag.Message = String.Format(ApiConstant.ERR90);
            }


            return View("~/Views/WS/Upload.cshtml", userStatus);
        }


        private bool csvValidationUserErr(IEnumerable<UserCsvRow> userCsvRow, out List<UserMaster> userList)
        {
            userList = new List<UserMaster>();

            var loginIdHs = new HashSet<string>();

            int line = 0;
            foreach (var user in userCsvRow)
            {
                ++line;

                //入力値のチェック
                if (string.IsNullOrWhiteSpace(user.LoginId) )
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR10, line, ApiConstant.LOGINID_JP);
                    return true;
                }
                if (user.LoginId.Length > ApiConstant.LOGINID_LENGTH_MAX)
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR12, line, ApiConstant.LOGINID_JP, ApiConstant.LOGINID_LENGTH_MAX);
                    return true;
                }
                if ( !Regex.IsMatch(user.LoginId, "^[0-9a-zA-Z]+$" ))
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR14, line, ApiConstant.LOGINID_JP);
                    return true;
                }

                if (string.IsNullOrWhiteSpace(user.LoginName)  )
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR10, line, ApiConstant.LOGINNAME_JP);
                    return true;
                }
                if ( user.LoginName.Length > ApiConstant.LOGINNAME_LENGTH_MAX)
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR12, line, ApiConstant.LOGINNAME_JP, ApiConstant.LOGINNAME_LENGTH_MAX);
                    return true;
                }


                if (string.IsNullOrWhiteSpace(user.Password))
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR10, line, ApiConstant.PASSWORD_JP);
                    return true;
                }
                if ( user.Password.Length > ApiConstant.PASSWORD_LENGTH_MAX)
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR12, line, ApiConstant.PASSWORD_JP, ApiConstant.PASSWORD_LENGTH_MAX);
                    return true;
                }
                if ( !Regex.IsMatch(user.Password, "^[0-9a-zA-Z]+$"))
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR14, line, ApiConstant.PASSWORD_JP);
                    return true;
                }

                //担当者ID重複チェック
                if (!loginIdHs.Add(user.LoginId))
                {
                    ViewBag.Message = String.Format(ApiConstant.ERR15, line, ApiConstant.LOGINID_JP);
                    return true;
                }

                var userMaster = _mapper.Map<UserMaster>(user);                
                userList.Add(userMaster);
            }


            return false;
        }
    }
}