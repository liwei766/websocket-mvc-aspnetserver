using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.Models
{
    /// <summary>
    /// センサー死活監視
    /// </summary>
    public class SensorMonitor
    {
        /// <summary>
        /// センサーID
        /// </summary>
        [Key]
        [MaxLength(4)]
        public string SensorId { get; set; }
        /// <summary>
        /// 最終受信時刻
        /// </summary>
        public DateTime LastReceiveTime { get; set; }
    }
}
