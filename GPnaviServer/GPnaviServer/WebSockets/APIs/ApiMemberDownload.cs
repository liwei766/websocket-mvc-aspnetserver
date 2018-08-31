using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets.APIs
{
    /// <summary>
    /// 担当者マスタダウンロード
    /// </summary>
    public class ApiMemberDownload : ApiCommonDown
    {
        /// <summary>
        /// バージョン
        /// </summary>
        public string version { get; set; }
        /// <summary>
        /// 担当者マスタ配列
        /// </summary>
        public List<Member> member_list { get; set; }
        /// <summary>
        /// 担当者マスタレコード
        /// </summary>
        public class Member
        {
            /// <summary>
            /// 担当者ID
            /// </summary>
            public string login_id { get; set; }
            /// <summary>
            /// 担当者名
            /// </summary>
            public string login_name { get; set; }
        }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ApiMemberDownload()
        {
            member_list = new List<Member>();
        }
    }
}
