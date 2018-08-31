using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.Models
{
    /// <summary>
    /// WS作業状態.
    /// 複合キーの設定はDBContextでFluent API設定する.
    /// </summary>
    public class WorkScheduleStatus
    {
        /// <summary>
        /// バージョン番号
        /// </summary>
        public long Version { get; set; }
        /// <summary>
        /// 作業開始時間
        /// </summary>
        [MaxLength(5)]
        public string Start { get; set; }
        /// <summary>
        /// 作業名
        /// </summary>
        [MaxLength(40)]
        public string Name { get; set; }
        /// <summary>
        /// 休日区分
        /// </summary>
        [MaxLength(1)]
        public string Holiday { get; set; }
        /// <summary>
        /// 作業状態
        /// </summary>
        [MaxLength(8)]
        public string Status { get; set; }
        /// <summary>
        /// 作業開始日付時刻
        /// </summary>
        public DateTime StartDate { get; set; }
        /// <summary>
        /// 作業状態更新日付時刻
        /// </summary>
        public DateTime StatusUpdateDate { get; set; }
        /// <summary>
        /// 担当者ID
        /// </summary>
        [MaxLength(13)]
        public string LoginId { get; set; }
    }
}
