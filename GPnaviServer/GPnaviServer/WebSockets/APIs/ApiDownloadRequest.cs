using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets.APIs
{
    /// <summary>
    /// マスタダウンロード要求
    /// </summary>
    public class ApiDownloadRequest : ApiCommonUp
    {
        /// <summary>
        /// マスタ種別
        /// </summary>
        public string type { get; set; }
    }
}
