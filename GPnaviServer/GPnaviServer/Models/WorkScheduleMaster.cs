using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.Models
{
    /// <summary>
    /// WSマスタ.
    /// 複合キーの設定はDBContextでFluent API設定する.
    /// </summary>
    public class WorkScheduleMaster
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
        /// 短縮作業名
        /// </summary>
        [MaxLength(20)]
        public string ShortName { get; set; }
        /// <summary>
        /// 重要度
        /// </summary>
        [MaxLength(1)]
        public string Priority { get; set; }
        /// <summary>
        /// 作業アイコンID
        /// </summary>
        [MaxLength(4)]
        public string IconId { get; set; }
        /// <summary>
        /// 標準作業時間
        /// </summary>
        [MaxLength(2)]
        public string Time { get; set; }
        /// <summary>
        /// 休日区分
        /// </summary>
        [MaxLength(1)]
        public string Holiday { get; set; }
        /// <summary>
        /// 行番号
        /// </summary>
        public long Row { get; set; }
    }
}
