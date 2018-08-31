using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets.APIs
{
    /// <summary>
    /// WSマスタダウンロード
    /// </summary>
    public class ApiWsDownload : ApiCommonDown
    {
        /// <summary>
        /// バージョン
        /// </summary>
        public string version { get; set; }
        /// <summary>
        /// WSマスタ配列
        /// </summary>
        public List<WorkSchedule> ws_list { get; set; }
        /// <summary>
        /// WSマスタレコード
        /// </summary>
        public class WorkSchedule
        {
            /// <summary>
            /// 作業開始時間
            /// </summary>
            public string ws_start { get; set; }
            /// <summary>
            /// 作業名
            /// </summary>
            public string ws_name { get; set; }
            /// <summary>
            /// 短縮作業名
            /// </summary>
            public string ws_short_name { get; set; }
            /// <summary>
            /// 重要度
            /// </summary>
            public string ws_priority { get; set; }
            /// <summary>
            /// 作業アイコンID
            /// </summary>
            public string ws_icon_id { get; set; }
            /// <summary>
            /// 標準作業時間
            /// </summary>
            public string ws_time { get; set; }
            /// <summary>
            /// 休日区分
            /// </summary>
            public string ws_holiday { get; set; }
            /// <summary>
            /// 行番号
            /// </summary>
            public string ws_row { get; set; }
        }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ApiWsDownload()
        {
            ws_list = new List<WorkSchedule>();
        }
    }
}
