using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.Models
{
    /// <summary>
    /// 担当者マスタ
    /// </summary>
    public class UserMaster
    {
        /// <summary>
        /// 担当者ID
        /// </summary>
        [Key]
        [MaxLength(13)]
        public string LoginId { get; set; }
        /// <summary>
        /// パスワード（ハッシュ後）
        /// </summary>
        [MaxLength(64)]
        public string Password { get; set; }
        /// <summary>
        /// 担当者名
        /// </summary>
        [MaxLength(16)]
        public string LoginName { get; set; }
        /// <summary>
        /// 権限 1:管理者／0:作業者
        /// </summary>
        [MaxLength(1)]
        public string Role { get; set; }
        /// <summary>
        /// 有効フラグ
        /// </summary>
        public bool IsValid { get; set; }
        /// <summary>
        /// 論理削除日付時刻k
        /// </summary>
        public DateTime RemoveDate { get; set; }
    }
}
