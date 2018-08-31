using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets.APIs
{
    /// <summary>
    /// 作業状況登録
    /// </summary>
    public class ApiRegister : ApiCommonUp
    {
        /// <summary>
        /// 作業状況配列
        /// </summary>
        public List<Work> work_list { get; set; }
        public class Work
        {
            /// <summary>
            /// 担当者ID
            /// </summary>
            public string login_id { get; set; }
            /// <summary>
            /// 登録日付時刻
            /// </summary>
            public string register_date { get; set; }
            /// <summary>
            /// 作業状態
            /// </summary>
            public string work_status { get; set; }
            /// <summary>
            /// 作業種別
            /// </summary>
            public string work_type { get; set; }
            /// <summary>
            /// バージョン（WS作業のみ）
            /// </summary>
            public string ws_version { get; set; }
            /// <summary>
            /// 作業開始時間（WS作業のみ）
            /// </summary>
            public string ws_start { get; set; }
            /// <summary>
            /// 作業名（WS作業のみ）
            /// </summary>
            public string ws_name { get; set; }
            /// <summary>
            /// 休日区分（WS作業のみ）
            /// </summary>
            public string ws_holiday { get; set; }
            /// <summary>
            /// レジFF種別（レジFF作業のみ）
            /// </summary>
            public string rf_type { get; set; }
            /// <summary>
            /// 応援フラグ（レジFF作業のみ）
            /// </summary>
            public string rf_help { get; set; }
            /// <summary>
            /// センサーID
            /// </summary>
            public string sensor_id { get; set; }
        }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ApiRegister()
        {
            work_list = new List<Work>();
        }
    }
}
