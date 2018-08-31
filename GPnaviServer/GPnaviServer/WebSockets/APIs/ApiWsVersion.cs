using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets.APIs
{
    /// <summary>
    /// WSマスタバージョン
    /// </summary>
    public class ApiWsVersion : ApiCommonDown
    {
        /// <summary>
        /// バージョン
        /// </summary>
        public string version { get; set; }
    }
}
