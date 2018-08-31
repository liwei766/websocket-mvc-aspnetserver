using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets.APIs
{
    /// <summary>
    /// ログイン
    /// </summary>
    public class ApiLogin : ApiCommonUp
    {
        /// <summary>
        /// パスワード
        /// </summary>
        public string password { get; set; }
        /// <summary>
        /// デバイストークン
        /// </summary>
        public string device_token { get; set; }
    }
}
