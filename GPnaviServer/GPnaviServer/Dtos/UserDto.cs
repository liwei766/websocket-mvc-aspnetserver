using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.Dtos
{
    public class UserDto
    {
        /// <summary>
        /// 担当者ID
        /// </summary>
        public string LoginId { get; set; }

        /// <summary>
        /// パスワード（ハッシュ後）
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 担当者名
        /// </summary>
        public string LoginName { get; set; }

        /// <summary>
        /// 権限 1:管理者／0:作業者
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// 論理削除日付時刻k
        /// </summary>
        public DateTime RemoveDate { get; set; }

        public string SessionKey { get; set; }
    }

    public class UserCsvRow
    {

        /// <summary>
        /// 担当者ID
        /// </summary>
        public string LoginId { get; set; }

        /// <summary>
        /// 担当者名
        /// </summary>
        public string LoginName { get; set; }

        /// <summary>
        /// パスワード（ハッシュ後）
        /// </summary>
        public string Password { get; set; }

    }
}
