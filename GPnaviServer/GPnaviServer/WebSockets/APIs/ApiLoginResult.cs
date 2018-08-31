using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets.APIs
{
    /// <summary>
    /// 認証結果
    /// </summary>
    public class ApiLoginResult : ApiCommonDown
    {
        /// <summary>
        /// 認証結果
        /// </summary>
        public string result { get; set; }
        /// <summary>
        /// エラーコード
        /// </summary>
        public string error_code { get; set; }
        /// <summary>
        /// エラーメッセージ
        /// </summary>
        public string error_message { get; set; }
        /// <summary>
        /// セッションキー
        /// </summary>
        public string session_key { get; set; }
        /// <summary>
        /// 権限
        /// </summary>
        public string role { get; set; }
    }
}
