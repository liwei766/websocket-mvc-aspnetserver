using System;
using System.Collections.Generic;
using System.Linq;
using GPnaviServer.Models;
using GPnaviServer.Helpers;
using GPnaviServer.Data;

namespace GPnaviServer.Services
{
    public interface IUserVersionService
    {

        long GetLatestVersion();
        UserVersion GetById(long id);
        long Add();
    }

    public class UserVersionService : IUserVersionService
    {
        private GPnaviServerContext _context;

        public UserVersionService(GPnaviServerContext context)
        {
            _context = context;
        }

        public long GetLatestVersion()
        {
            if (null == _context.UserVersions.FirstOrDefault())
            {
                return 0;
            }
                
            return _context.UserVersions.Max(e => e.Id);
        }

        public UserVersion GetById(long id)
        {
            return _context.UserVersions.Find(id);
        }

        /// <summary>
        /// 登録済みの最新のユーザマスタバージョンを検索し、存在する場合は有効期限日付時刻を現在時刻で更新する。ユーザマスタバージョンを追加してバージョン番号を取得する。
        /// </summary>
        public long Add()
        {
            UserVersion wsv = new UserVersion();
            wsv.ExpirationDate = DateTime.MaxValue;
            var now = DateTime.Now;
            wsv.RegisterDate = now;

            long latestVersion = GetLatestVersion();
            if(latestVersion>0)
            {
                UserVersion latestUserVer = _context.UserVersions.Find(GetLatestVersion());
                latestUserVer.ExpirationDate = now;
                _context.UserVersions.Update(latestUserVer);
            }

            _context.UserVersions.Add(wsv);
            _context.SaveChanges();

            return GetLatestVersion();
        }

    }
}
