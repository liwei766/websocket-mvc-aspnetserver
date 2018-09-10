using System;
using System.Collections.Generic;
using System.Linq;
using GPnaviServer.Models;
using GPnaviServer.Helpers;
using GPnaviServer.Data;

namespace GPnaviServer.Services
{
    public interface IWorkScheduleService
    {
        int Add(List<WorkScheduleMaster> wsmList);
    }

    public class WorkScheduleService : IWorkScheduleService
    {
        private GPnaviServerContext _context;

        public WorkScheduleService(GPnaviServerContext context)
        {
            _context = context;
        }



        public WorkScheduleMaster GetById(long id)
        {
            return _context.WorkScheduleMasters.Find(id);
        }

        /// <summary>
        /// 登録済みの最新のWSマスタバージョンを検索し、存在する場合は有効期限日付時刻を現在時刻で更新する。WSマスタバージョンを追加してバージョン番号を取得する。
        /// </summary>
        public int Add(List<WorkScheduleMaster> wsmList)
        {

            using (var transaction = _context.Database.BeginTransaction())
            {
                WorkScheduleVersion wsv = new WorkScheduleVersion();
                wsv.ExpirationDate = DateTime.MaxValue;
                var now = DateTime.Now;
                wsv.RegisterDate = now;

                if (_context.WorkScheduleVersions.Count() > 0)
                {
                    WorkScheduleVersion latestWsv = _context.WorkScheduleVersions.OrderByDescending(e => e.Id).First();
                    latestWsv.ExpirationDate = now;
                    _context.WorkScheduleVersions.Update(latestWsv);
                }

                _context.WorkScheduleVersions.Add(wsv);
                _context.SaveChanges();

                long latestVer = _context.WorkScheduleVersions.Max(e => e.Id);

                //DB WSマスタ追加
                wsmList.ForEach(wsm => { wsm.Version = latestVer; _context.WorkScheduleMasters.Add(wsm); });
                _context.SaveChanges();

                transaction.Commit();

                return wsmList.Count();
            }
        }

    }
}
