using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.Models
{
    /// <summary>
    /// センサーマスタ
    /// </summary>
    public class SensorMaster
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
    }
}
