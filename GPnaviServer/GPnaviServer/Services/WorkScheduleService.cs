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
            wsmList.ForEach(wsm => _context.WorkScheduleMasters.Add(wsm));
            _context.SaveChanges();

            return wsmList.Count();
        }

    }
}
