using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets.APIs
{
    /// <summary>
    /// レジFF応援（Push）
    /// </summary>
    public class ApiHelpPush : ApiCommonDown
    {
        /// <summary>
        /// レジFF種別
        /// </summary>
        public string rf_type { get; set; }
        /// <summary>
        /// 担当者ID
        /// </summary>
        public string login_id { get; set; }
    }
}
