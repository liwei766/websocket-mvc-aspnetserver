using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets.APIs
{
    /// <summary>
    /// 登録結果
    /// </summary>
    public class ApiRegisterResult : ApiCommonDown
    {
        /// <summary>
        /// 登録結果
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
    }
}
