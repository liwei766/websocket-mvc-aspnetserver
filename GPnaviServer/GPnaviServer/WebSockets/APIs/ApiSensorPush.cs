using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets.APIs
{
    /// <summary>
    /// センサー検知
    /// </summary>
    public class ApiSensorPush : ApiCommonDown
    {
        /// <summary>
        /// デバイス区分
        /// </summary>
        public string sensor_type { get; set; }
        /// <summary>
        /// センサーID
        /// </summary>
        public string sensor_id { get; set; }
        /// <summary>
        /// 表示メッセージ
        /// </summary>
        public string display_message { get; set; }
        /// <summary>
        /// 発生日付時刻
        /// </summary>
        public string date { get; set; }
    }
}
