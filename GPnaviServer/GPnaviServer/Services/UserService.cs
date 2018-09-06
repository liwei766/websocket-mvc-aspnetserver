using System;
using System.Collections.Generic;
using System.Linq;
using GPnaviServer.Models;
using GPnaviServer.Helpers;
using System.Security.Cryptography;
using System.Text;
using GPnaviServer.Data;
using Microsoft.EntityFrameworkCore;
using GPnaviServer.WebSockets.APIs;

namespace GPnaviServer.Services
{
    public interface IUserService
    {
        UserMaster Authenticate(string loginId, string password);
        IEnumerable<UserMaster> GetAll();
        UserMaster GetById(string id);
        UserMaster Create(UserMaster user, string password);
        int Upload(List<UserMaster> userList);
    }

    public class UserService : IUserService
    {
        private GPnaviServerContext _context;

        public UserService(GPnaviServerContext context)
        {
            _context = context;
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

        public int Upload(List<UserMaster> userList)
        {
            var changedUserlist = new List<UserMaster>();
            userList.ForEach(user =>
            {
                user.Role = ApiConstant.ROLE_WORK;
                user.IsValid = true;
                user.Password = CreatePasswordHash(user.Password);
                user.RemoveDate = DateTime.MaxValue;

                UserMaster dbUser = GetById(user.LoginId);
                if (null == dbUser)
                {
                    _context.UserMasters.Add(user);
                }
                else
                {
                    dbUser.LoginName = user.LoginName;
                    dbUser.Role = user.Role;
                    dbUser.IsValid = user.IsValid;
                    dbUser.Password = user.Password;
                    dbUser.RemoveDate = user.RemoveDate;
                }
                changedUserlist.Add(dbUser);

            });
            _context.SaveChanges();

            var allUsers = _context.UserMasters.Where(userdb => userdb.IsValid && userdb.Role== ApiConstant.ROLE_WORK && userdb.RemoveDate == DateTime.MaxValue ).ToArray();

            var expiredUserList = allUsers.Except(changedUserlist);

            var now = DateTime.Now;
            foreach (var expiredUser in expiredUserList) {
                expiredUser.IsValid = false;
                expiredUser.RemoveDate = now;
            };

            _context.SaveChanges();


            return userList.Count();
        }


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

        public IEnumerable<UserMaster> GetAll()
        {
            return _context.UserMasters;
        }

        public UserMaster GetById(string id)
        {
            return _context.UserMasters.Find(id);
        }

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

        // private helper methods

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

        private byte[] GetKeyByteArray(string strKey)
        {
            UTF8Encoding Asc = new UTF8Encoding();
            int tmpStrLen = strKey.Length;
            byte[] tmpByte = new byte[tmpStrLen - 1];
            tmpByte = Asc.GetBytes(strKey);
            return tmpByte;
        }

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
