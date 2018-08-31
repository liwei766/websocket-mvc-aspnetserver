using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets.APIs
{
    /// <summary>
    /// 共通部分上り方向
    /// </summary>
    public class ApiCommonUp
    {
        /// <summary>
        /// メッセージ名
        /// </summary>
        public string message_name { get; set; }
        /// <summary>
        /// デバイス区分（ANDROID/IOT）
        /// </summary>
        public string device_type { get; set; }
        /// <summary>
        /// 担当者ID
        /// </summary>
        public string login_id { get; set; }
        /// <summary>
        /// セッションキー
        /// </summary>
        public string session_key { get; set; }
    }
}
