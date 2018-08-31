using System;
using System.Collections.Generic;
using System.Linq;
using GPnaviServer.Models;
using GPnaviServer.Helpers;
using GPnaviServer.Data;

namespace GPnaviServer.Services
{
    public interface IWorkScheduleVersionService
    {

        long GetLatestVersion();
        WorkScheduleVersion GetById(long id);
        long Add();
    }

    public class WorkScheduleVersionService : IWorkScheduleVersionService
    {
        private GPnaviServerContext _context;

        public WorkScheduleVersionService(GPnaviServerContext context)
        {
            _context = context;
        }

        public long GetLatestVersion()
        {
            if (null == _context.WorkScheduleVersions.FirstOrDefault())
            {
                return 0;
            }
                
            return _context.WorkScheduleVersions.Max(e => e.Id);
        }

        public WorkScheduleVersion GetById(long id)
        {
            return _context.WorkScheduleVersions.Find(id);
        }

        /// <summary>
        /// 登録済みの最新のWSマスタバージョンを検索し、存在する場合は有効期限日付時刻を現在時刻で更新する。WSマスタバージョンを追加してバージョン番号を取得する。
        /// </summary>
        public long Add()
        {
            WorkScheduleVersion wsv = new WorkScheduleVersion();
            wsv.ExpirationDate = DateTime.MaxValue;
            var now = DateTime.Now;
            wsv.RegisterDate = now;

            long latestVersion = GetLatestVersion();
            if(latestVersion>0)
            {
                WorkScheduleVersion latestWsv = _context.WorkScheduleVersions.Find(GetLatestVersion());
                latestWsv.ExpirationDate = now;
                _context.WorkScheduleVersions.Update(latestWsv);
            }

            _context.WorkScheduleVersions.Add(wsv);
            _context.SaveChanges();

            return GetLatestVersion();
        }

    }
}
