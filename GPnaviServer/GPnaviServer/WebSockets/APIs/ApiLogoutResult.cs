using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets.APIs
{
    /// <summary>
    /// ログアウト結果
    /// </summary>
    public class ApiLogoutResult : ApiCommonDown
    {
        /// <summary>
        /// ログアウト結果
        /// </summary>
        public string result { get; set; }
    }
}
