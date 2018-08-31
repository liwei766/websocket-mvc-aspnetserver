using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets.APIs
{
    /// <summary>
    /// 祝日マスタダウンロード
    /// </summary>
    public class ApiHolidayDownload : ApiCommonDown
    {
        /// <summary>
        /// 祝日情報配列
        /// </summary>
        public List<string> holiday_list { get; set; }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ApiHolidayDownload()
        {
            holiday_list = new List<string>();
        }
    }
}
