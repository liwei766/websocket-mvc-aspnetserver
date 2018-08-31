using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets.APIs
{
    /// <summary>
    /// 担当者マスタバージョン
    /// </summary>
    public class ApiMemberVersion : ApiCommonDown
    {
        /// <summary>
        /// バージョン
        /// </summary>
        public string version { get; set; }
    }
}
