using System;
using System.Collections.Generic;
using System.Linq;
using GPnaviServer.Models;
using GPnaviServer.Helpers;
using GPnaviServer.Data;

namespace GPnaviServer.Services
{
    public interface IUserStatusService
    {

        IEnumerable<UserStatus> GetAll();
        UserStatus GetById(string id);
        UserStatus Create(string loginId, string sessionKey);
        UserStatus UpdateOrCreate(string loginId, string sessionKey);
        UserStatus ClearSessionKey(string loginId, string sessionKey);
    }

    public class UserStatusService : IUserStatusService
    {
        private GPnaviServerContext _context;

        public UserStatusService(GPnaviServerContext context)
        {
            _context = context;
        }

        public IEnumerable<UserStatus> GetAll()
        {
            return _context.UserStatuses;
        }

        public UserStatus GetById(string id)
        {
            return _context.UserStatuses.Find(id);
        }

        public UserStatus Create(string loginId, string sessionKey)
        {
            // validation
            if (string.IsNullOrWhiteSpace(loginId))
                throw new AppException("loginId is required");

            if (string.IsNullOrWhiteSpace(sessionKey))
                throw new AppException("sessionKey is required");

            UserStatus userStatus = new UserStatus();

            userStatus.LoginId = loginId;
            userStatus.SessionKey = sessionKey;

            _context.UserStatuses.Add(userStatus);
            _context.SaveChanges();

            return userStatus;
        }

        public UserStatus UpdateOrCreate(string loginId, string sessionKey)
        {
            // validation
            if (string.IsNullOrWhiteSpace(loginId))
                throw new AppException("loginId is required");

            if (string.IsNullOrWhiteSpace(sessionKey))
                throw new AppException("sessionKey is required");

            UserStatus userStatus = GetById(loginId);


            if (userStatus == null)
            {
                userStatus = Create(loginId, sessionKey);
            }
            else
            {
                userStatus.SessionKey = sessionKey;
                _context.UserStatuses.Update(userStatus);
                _context.SaveChanges();
            }

            return userStatus;
        }

        public UserStatus ClearSessionKey(string loginId, string sessionKey)
        {
            var userStatus = GetById(loginId);
            if (userStatus != null || userStatus.SessionKey != null || string.Equals(sessionKey, userStatus.SessionKey))
            {
                userStatus.SessionKey = "";
                _context.SaveChanges();
            }
            return userStatus;
        }


    }
}
