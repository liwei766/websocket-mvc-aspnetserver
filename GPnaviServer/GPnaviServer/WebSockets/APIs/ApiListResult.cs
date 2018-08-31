using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets.APIs
{
    /// <summary>
    /// 作業状況一覧
    /// </summary>
    public class ApiListResult : ApiCommonDown
    {
        /// <summary>
        /// 作成結果
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
        /// <summary>
        /// 作業状況配列
        /// </summary>
        public List<WorkItem> work_list { get; set; }
    }
    /// <summary>
    /// 作業状況アイテム
    /// </summary>
    public class WorkItem
    {
        /// <summary>
        /// 表示用日付時刻
        /// </summary>
        public string display_date { get; set; }
        /// <summary>
        /// WSバージョン番号
        /// </summary>
        public string ws_version { get; set; }
        /// <summary>
        /// WS作業開始時間
        /// </summary>
        public string ws_start { get; set; }
        /// <summary>
        /// WS作業名
        /// </summary>
        public string ws_name { get; set; }
        /// <summary>
        /// WS休日区分
        /// </summary>
        public string ws_holiday { get; set; }
        /// <summary>
        /// WS重要度
        /// </summary>
        public string ws_priority { get; set; }
        /// <summary>
        /// センサーID
        /// </summary>
        public string sensor_id { get; set; }
        /// <summary>
        /// センサー区分
        /// </summary>
        public string sensor_type { get; set; }
        /// <summary>
        /// レジFF種別
        /// </summary>
        public string rf_type { get; set; }
        /// <summary>
        /// WS作業アイコンID
        /// </summary>
        public string ws_icon_id { get; set; }
        /// <summary>
        /// 表示用作業名
        /// </summary>
        public string display_short_name { get; set; }
        /// <summary>
        /// 担当者ID
        /// </summary>
        public string login_id { get; set; }
        /// <summary>
        /// 担当者名
        /// </summary>
        public string login_name { get; set; }
        /// <summary>
        /// ステータス
        /// </summary>
        public string work_status { get; set; }
        /// <summary>
        /// 突発発生日付時刻
        /// </summary>
        public string sensor_date { get; set; }
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
