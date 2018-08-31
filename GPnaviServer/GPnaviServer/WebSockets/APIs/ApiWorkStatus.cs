using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets.APIs
{
    /// <summary>
    /// 作業ステータス
    /// </summary>
    public class ApiWorkStatus : ApiCommonDown
    {
        /// <summary>
        /// 作業状態（WS作業のみ）
        /// </summary>
        public string work_status { get; set; }
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
        /// センサーID（突発作業のみ）
        /// </summary>
        public string sensor_id { get; set; }
        /// <summary>
        /// 作業開始日付時刻
        /// </summary>
        public string start_date { get; set; }
        /// <summary>
        /// 登録日付時刻
        /// </summary>
        public string register_date { get; set; }
    }
}
