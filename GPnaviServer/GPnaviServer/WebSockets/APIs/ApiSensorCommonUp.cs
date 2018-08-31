using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets.APIs
{
    /// <summary>
    /// MQTT センサー共通部分上り方向
    /// </summary>
    public class ApiSensorCommonUp
    {
        /// <summary>
        /// メッセージ名
        /// </summary>
        public string message_name { get; set; }
        /// <summary>
        /// センサーID
        /// </summary>
        public string sensor_id { get; set; }
    }
}
