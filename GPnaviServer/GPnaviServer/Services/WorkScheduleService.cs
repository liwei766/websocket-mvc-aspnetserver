using System;
using System.Collections.Generic;
using System.Linq;
using GPnaviServer.Models;
using GPnaviServer.Helpers;
using GPnaviServer.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GPnaviServer.Services
{
    public interface IWorkScheduleService
    {
        Task UploadAsync(List<WorkScheduleMaster> wsmList);
    }

    public class WorkScheduleService : IWorkScheduleService
    {
        /// <summary>
        /// DBコンテキスト
        /// </summary>
        private GPnaviServerContext _context;
        /// <summary>
        /// ロガー
        /// </summary>
        protected readonly ILogger _logger;

        public WorkScheduleService(GPnaviServerContext context, ILogger<WorkScheduleService> logger)
        {
            _context = context;
            _logger = logger;
        }



        public WorkScheduleMaster GetById(long id)
        {
            return _context.WorkScheduleMasters.Find(id);
        }

        /// <summary>
        /// WS情報をアップロードして、一括登録
        /// </summary>
        /// <param name="userList">一括登録のWSマスタリスト</param>
        /// <returns></returns>
        public async Task UploadAsync(List<WorkScheduleMaster> wsmList)
        {
            _logger.LogTrace(DateTime.Now + "|DBへWS一括登録処理開始");
            using (var transaction = _context.Database.BeginTransaction())
            {
                try {
                    // 登録済みの最新のWSマスタバージョンを検索し、存在する場合は有効期限日付時刻を現在時刻で更新する。WSマスタバージョンを追加してバージョン番号を取得する。
                    var wsv = new WorkScheduleVersion();
                    wsv.ExpirationDate = DateTime.MaxValue;
                    var now = DateTime.Now;
                    wsv.RegisterDate = now;

                    if (_context.WorkScheduleVersions.Any())
                    {
                        var latestWsv = _context.WorkScheduleVersions.OrderByDescending(e => e.Id).First();
                        latestWsv.ExpirationDate = now;
                    }

                    _context.WorkScheduleVersions.Add(wsv);
                    await _context.SaveChangesAsync();

                    long latestVer = _context.WorkScheduleVersions.Max(e => e.Id);
                    _logger.LogTrace(DateTime.Now + $"|WSマスタバージョン更新済。最新バージョン：{latestVer}");

                    //DBにWSマスタを追加
                    wsmList.ForEach(wsm => { wsm.Version = latestVer; _context.WorkScheduleMasters.Add(wsm); });
                    await _context.SaveChangesAsync();

                    transaction.Commit();
                    _logger.LogTrace(DateTime.Now + $"|WS一括登録済：合計{wsmList.Count}件");
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    _logger.LogError(DateTime.Now + "|処理失敗、ロールバック済。Exceptionメッセージ：" + e.Message);
                    throw;
                }
            }
        }

    }
}
