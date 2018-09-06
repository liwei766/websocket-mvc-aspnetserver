﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GPnaviServer.Services;
using GPnaviServer.Dtos;
using AutoMapper;
using GPnaviServer.Helpers;
using Microsoft.AspNetCore.Authorization;

using GPnaviServer.Models;
using Microsoft.AspNetCore.Http;
using GPnaviServer.WebSockets.APIs;
using System.Threading.Tasks;
using System.IO;
using CsvHelper;
using System.Text.RegularExpressions;

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



        public IActionResult Home(UserDto userDto)
        {
            if (userDto==null || string.IsNullOrEmpty(userDto.LoginId))
            {
                return View("Login"); 
            }

            if(userDto!=null && !string.IsNullOrEmpty(userDto.LoginId) && !string.IsNullOrEmpty(userDto.SessionKey))
            {
                var status = _userStatusService.GetById(userDto.LoginId);
                if (status!=null && string.Equals(userDto.SessionKey,status.SessionKey))
                {
                    ViewBag.LoginName = _userService.GetById(userDto.LoginId).LoginName;
                    return View("Home", status);
                }
            }

            var userMaster = _mapper.Map<UserMaster>(userDto);

            //パースワードチェック
            var user = _userService.Authenticate(userDto.LoginId, userDto.Password);

            if (user == null)
            {
                ViewBag.Message = "ログイン名またはパスワードが間違っています";
                return View("Login", userMaster);
            }

            //役割チェック　ROLE_ADMIN = "1"　ROLE_WORK = "0";
            if (!string.Equals(user.Role, ApiConstant.ROLE_ADMIN))
            {
                ViewBag.Message = "管理者ではありません";
                return View("Login", userMaster);
            }


            // 担当者状態のセッションキーチェック
            var userStatus = _userStatusService.GetById(user.LoginId);
            if( string.Equals(userDto.SessionKey, "undefined") || userStatus==null || !string.Equals( userDto.SessionKey, userStatus.SessionKey))
            {
                string sessionKey = HttpContext.Session.Id;
                HttpContext.Session.SetString("SessionKey", sessionKey);
                userStatus = _userStatusService.UpdateOrCreate(user.LoginId, sessionKey);
            }
            ViewBag.LoginName = user.LoginName;

            return View("Home", userStatus);
        }


        public IActionResult Logout(UserDto userDto)
        {
            if (userDto==null || userDto.LoginId ==null || userDto.SessionKey==null)
            {
                return View("Login");
            }

            _userStatusService.ClearSessionKey(userDto.LoginId, userDto.SessionKey);
            
            return View("Login");
        }

        [HttpPost("uploaduserdata")]
        public async Task<IActionResult> uploadUserData(IFormFile file, string LoginId, string SessionKey)
        {
            UserStatus userStatus = null;
            if (string.IsNullOrEmpty(LoginId) || string.IsNullOrEmpty(SessionKey))
            {
                return View("~/Views/Users/Login.cshtml");
            }
            else
            {
                userStatus = _userStatusService.GetById(LoginId);
                if (userStatus == null || !string.Equals(SessionKey, userStatus.SessionKey))
                {
                    return View("~/Views/Users/Login.cshtml");
                }
            }

            if (file.Length<1)
            {
                return View("~/Views/WS/Upload.cshtml", userStatus);
            }

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

                ViewBag.Message = "ユーザ登録OK:合計" + countAdd + "件追加済";

            }


            return View("~/Views/WS/Upload.cshtml", userStatus);
        }


        //public IActionResult Register([FromBody]UserDto userDto)
        //{
        //    // map dto to entity
        //    var user = _mapper.Map<UserMaster>(userDto);

        //    try
        //    {
        //        // save 
        //        _userService.Create(user, userDto.Password);
        //        return Ok();
        //    }
        //    catch (AppException ex)
        //    {
        //        // return error message if there was an exception
        //        return BadRequest(ex.Message);
        //    }
        //}

        //[HttpGet("allusers")]
        //public IActionResult GetAll()
        //{
        //    var users = _userService.GetAll();
        //    var userDtos = _mapper.Map<IList<UserDto>>(users);
        //    return Ok(userDtos);
        //}

        private bool csvValidationUserErr(IEnumerable<UserCsvRow> userCsvRow, out List<UserMaster> userList)
        {
            userList = new List<UserMaster>();

            var loginIdHs = new HashSet<string>();

            //ユーザーIDの最大文字数
            const int LOGINID_LENGTH_MAX = 3;

            //ユーザー名の最大文字数
            const int LOGINNAME_LENGTH_MAX = 16;

            //パースワード文字数
            const int PASSWORD_LENGTH_MAX = 4;


            int line = 0;
            foreach (var user in userCsvRow)
            {
                ++line;

                //入力値のチェック
                //{N}行目の{項目名}がありません
                if (string.IsNullOrWhiteSpace(user.LoginId) )
                {
                    ViewBag.Message = $"{line}行目の担当者IDがありません";
                    return true;
                }
                if (user.LoginId.Length > LOGINID_LENGTH_MAX)
                {
                    ViewBag.Message = $"{line}行目の担当者IDの文字数が3をこえています";
                    return true;
                }
                if ( !Regex.IsMatch(user.LoginId, "^[0-9a-zA-Z]+$" ))
                {
                    ViewBag.Message = $"{line}行目の担当者IDは半角英数字を使用してください";
                    return true;
                }

                if (string.IsNullOrWhiteSpace(user.LoginName)  )
                {
                    ViewBag.Message = $"{line}行目の担当者名がありません";
                    return true;
                }
                if ( user.LoginName.Length > LOGINNAME_LENGTH_MAX)
                {
                    ViewBag.Message = $"{line}行目の担当者名の文字数が16をこえています";
                    return true;
                }


                if (string.IsNullOrWhiteSpace(user.Password))
                {
                    ViewBag.Message = $"{line}行目のパスワードがありません";
                    return true;
                }
                if ( user.Password.Length > PASSWORD_LENGTH_MAX)
                {
                    ViewBag.Message = $"{line}行目のパスワードの文字数が4をこえています";
                    return true;
                }
                if ( !Regex.IsMatch(user.LoginId, "^[0-9a-zA-Z]+$"))
                {
                    ViewBag.Message = $"{line}行目のパスワードは半角英数字を使用してください";
                    return true;
                }

                //担当者ID重複チェック
                if (!loginIdHs.Add(user.LoginId))
                {
                    ViewBag.Message = $"{line}行目の担当者IDが重複しています";
                    return true;
                }

                var userMaster = _mapper.Map<UserMaster>(user);                
                userList.Add(userMaster);
            }


            return false;
        }
    }
}