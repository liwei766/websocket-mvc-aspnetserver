using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.Models
{
    /// <summary>
    /// 突発作業状態
    /// </summary>
    public class SensorStatus
    {
        /// <summary>
        /// センサーID
        /// </summary>
        [Key]
        [MaxLength(4)]
        public string SensorId { get; set; }
        /// <summary>
        /// センサーデバイス区分
        /// </summary>
        [MaxLength(8)]
        public string SensorType { get; set; }
        /// <summary>
        /// 表示用作業名
        /// </summary>
        [MaxLength(20)]
        public string DisplayName { get; set; }
        /// <summary>
        /// 突発発生日付時刻
        /// </summary>
        public DateTime OccurrenceDate { get; set; }
        /// <summary>
        /// 作業開始日付時刻
        /// </summary>
        public DateTime StartDate { get; set; }
        /// <summary>
        /// 作業状態
        /// </summary>
        [MaxLength(8)]
        public string Status { get; set; }
        /// <summary>
        /// 担当者ID
        /// </summary>
        [MaxLength(13)]
        public string LoginId { get; set; }
    }
}
