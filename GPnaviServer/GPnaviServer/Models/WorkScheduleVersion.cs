using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.Models
{
    /// <summary>
    /// WSマスタバージョン管理
    /// </summary>
    public class WorkScheduleVersion
    {
        /// <summary>
        /// バージョン番号.
        /// 自動採番主キーを使用するためデフォルト名を採用.
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// 登録日付時刻
        /// </summary>
        public DateTime RegisterDate { get; set; }
        /// <summary>
        /// 有効期限日付時刻
        /// </summary>
        public DateTime ExpirationDate { get; set; }
    }
}
