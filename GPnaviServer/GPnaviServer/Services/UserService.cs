using System;
using System.Collections.Generic;
using System.Linq;
using GPnaviServer.Models;
using GPnaviServer.Helpers;
using System.Security.Cryptography;
using System.Text;
using GPnaviServer.Data;
using GPnaviServer.WebSockets.APIs;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GPnaviServer.Services
{
    public interface IUserService
    {
        UserMaster Authenticate(string loginId, string password);
        UserMaster GetById(string id);
        UserMaster Create(UserMaster user, string password);
        Task UploadAsync(List<UserMaster> userList);
    }

    public class UserService : IUserService
    {
        /// <summary>
        /// DBコンテキスト
        /// </summary>
        private GPnaviServerContext _context;
        /// <summary>
        /// ロガー
        /// </summary>
        protected readonly ILogger _logger;

        public UserService(GPnaviServerContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public int Add(List<UserMaster> userList)
        {
            userList.ForEach(user =>
            {
                user.Role = ApiConstant.ROLE_WORK;
                user.IsValid = true;
                user.Password = CreatePasswordHash(user.Password);
                user.RemoveDate = DateTime.MaxValue;
                _context.UserMasters.Add(user);
            });

            _context.SaveChanges();

            return userList.Count();
        }

        /// <summary>
        /// 担当者情報をアップロードして、一括登録
        /// </summary>
        /// <param name="userList">一括登録のユーザリスト</param>
        /// <returns></returns>
        public async Task UploadAsync(List<UserMaster> userList)
        {
            _logger.LogTrace(DateTime.Now + "|DBへ担当者一括登録処理開始");
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var uv = new UserVersion();
                    uv.ExpirationDate = DateTime.MaxValue;
                    var now = DateTime.Now;
                    uv.RegisterDate = now;

                    if (_context.UserVersions.Any())
                    {
                        var latestWsv = _context.UserVersions.OrderByDescending(e => e.Id).First();
                        latestWsv.ExpirationDate = now;
                    }
                    _context.UserVersions.Add(uv);
                    _logger.LogTrace(DateTime.Now + $"|ユーザバージョン更新済。");


                    var changedUserlist = new List<UserMaster>();
                    userList.ForEach(user =>
                    {
                        //DBに新規作成又は更新前に、部分コラムにデフォルト値を設定
                        user.Role = ApiConstant.ROLE_WORK;
                        user.IsValid = true;
                        user.Password = CreatePasswordHash(user.Password);
                        user.RemoveDate = DateTime.MaxValue;

                        UserMaster dbUser = GetById(user.LoginId);
                        if (null == dbUser)//存在しなければ新規作成
                        {
                            _context.UserMasters.Add(user);
                            dbUser = user;
                        }
                        else//既存のデータを更新
                        {
                            dbUser.LoginName = user.LoginName;
                            dbUser.Role = user.Role;
                            dbUser.IsValid = user.IsValid;
                            dbUser.Password = user.Password;
                            dbUser.RemoveDate = user.RemoveDate;
                        }
                        changedUserlist.Add(dbUser);
                    });
                    _logger.LogTrace(DateTime.Now + $"|ユーザマスタにADD又はUPDATEで更新済：合計{userList.Count}件");

                    //全て有効的な普通のユーザを取得
                    var allUsers = _context.UserMasters.Where(userdb => userdb.IsValid && userdb.Role== ApiConstant.ROLE_WORK && userdb.RemoveDate == DateTime.MaxValue );

                    //今回一括登録されたユーザリストに含まれていないユーザを取得
                    var expiredUserList = allUsers.Except(changedUserlist);

                    //論理削除
                    foreach (var expiredUser in expiredUserList) {
                        expiredUser.IsValid = false;
                        expiredUser.RemoveDate = now;
                    };
                    _logger.LogTrace(DateTime.Now + $"|ユーザマスタに論理削除の更新済：合計{expiredUserList.Count()}件");

                    //非同期で保存
                    await _context.SaveChangesAsync();
                    transaction.Commit();
                    _logger.LogTrace(DateTime.Now + $"|担当者一括登録済：合計{userList.Count}件");
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    _logger.LogError(DateTime.Now + "|処理失敗、ロールバック済。Exceptionメッセージ：" + e.Message);
                    throw;
                }
            }
        }

        /// <summary>
        /// ユーザ認証
        /// </summary>
        /// <param name="loginId">ユーザID</param>
        /// <param name="password">ユーザパースワード</param>
        /// <returns>認証済のユーザ</returns>
        public UserMaster Authenticate(string loginId, string password)
        {
            if (string.IsNullOrEmpty(loginId) || string.IsNullOrEmpty(password))
                return null;

            var user = _context.UserMasters.SingleOrDefault(x => x.LoginId == loginId && x.IsValid);

            // check if username exists
            if (user == null)
                return null;

            // check if password is correct
            if (!VerifyPasswordHash(password, user.Password))
                return null;

            // authentication successful
            return user;
        }


        /// <summary>
        /// ユーザIDで該当ユーザを取得
        /// </summary>
        /// <param name="loginId">ユーザID</param>
        /// <returns>取得されたユーザ</returns>
        public UserMaster GetById(string id)
        {
            return _context.UserMasters.Find(id);
        }

        /// <summary>
        /// ユーザを新規作成
        /// </summary>
        /// <param name="user">ユーザ情報</param>
        /// <param name="password">ユーザパースワード</param>
        /// <returns>新規作成したユーザ</returns>
        public UserMaster Create(UserMaster user, string password)
        {
            // validation
            if (string.IsNullOrWhiteSpace(password))
                throw new AppException("Password is required");

            if (_context.UserMasters.Any(x => x.LoginId == user.LoginId))
                throw new AppException("LoginName '" + user.LoginId + "' is already taken");

            user.Password = CreatePasswordHash(password);

            _context.UserMasters.Add(user);
            _context.SaveChanges();

            return user;
        }

        /// <summary>
        /// パースワード暗号化
        /// </summary>
        /// <param name="password">普通のパースワード</param>
        /// <returns>暗号化したパースワード</returns>
        public string CreatePasswordHash(string password)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

            byte[] tmpByte;
            SHA256 sha256 = new SHA256Managed();
            tmpByte = sha256.ComputeHash(GetKeyByteArray(password));

            StringBuilder rst = new StringBuilder();
            for (int i = 0; i < tmpByte.Length; i++)
            {
                rst.Append(tmpByte[i].ToString("x2"));
            }
            sha256.Clear();
            return rst.ToString();
        }

        /// <summary>
        /// stringからbyte arrayに変換
        /// </summary>
        /// <param name="strKey">入力のストリング</param>
        /// <returns>変換済のbyte array</returns>
        private byte[] GetKeyByteArray(string strKey)
        {
            UTF8Encoding Asc = new UTF8Encoding();
            int tmpStrLen = strKey.Length;
            byte[] tmpByte = new byte[tmpStrLen - 1];
            tmpByte = Asc.GetBytes(strKey);
            return tmpByte;
        }

        /// <summary>
        /// パースワードバリデーションチェック
        /// </summary>
        /// <param name="password">入力のパースワード</param>
        /// <param name="storedPasswordHash">DBに保存している暗号化のパースワード</param>
        /// <returns>バリデーションチェック結果</returns>
        private bool VerifyPasswordHash(string password, string storedPasswordHash)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            //if (storedHash.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");

            string passwordHash = CreatePasswordHash(password);
            if (storedPasswordHash != passwordHash)
                return false;

            return true;
        }
    }
}
