using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.Models
{
    /// <summary>
    /// 作業状況履歴.
    /// 複合キーの設定はDBContextでFluent API設定する.
    /// </summary>
    public class WorkStatusHistory
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
        /// レジFF種別
        /// </summary>
        [MaxLength(5)]
        public string RfType { get; set; }
        /// <summary>
        /// センサーID
        /// </summary>
        [MaxLength(4)]
        public string SensorId { get; set; }
        /// <summary>
        /// 表示用作業名
        /// </summary>
        [MaxLength(20)]
        public string DisplayName { get; set; }
        /// <summary>
        /// 担当者ID
        /// </summary>
        [MaxLength(13)]
        public string LoginId { get; set; }
        /// <summary>
        /// 担当者名
        /// </summary>
        [MaxLength(16)]
        public string LoginName { get; set; }
        /// <summary>
        /// 作業ステータス
        /// </summary>
        [MaxLength(8)]
        public string Status { get; set; }
        /// <summary>
        /// 作業開始日付時刻
        /// </summary>
        public DateTime StartDate { get; set; }
        /// <summary>
        /// 登録日付時刻
        /// </summary>
        public DateTime RegisterDate { get; set; }
    }
}
