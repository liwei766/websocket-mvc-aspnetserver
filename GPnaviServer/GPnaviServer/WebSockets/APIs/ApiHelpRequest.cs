using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets.APIs
{
    /// <summary>
    /// レジFF応援
    /// </summary>
    public class ApiHelpRequest : ApiCommonUp
    {
        /// <summary>
        /// レジFF種別
        /// </summary>
        public string rf_type { get; set; }
    }
}
