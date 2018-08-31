using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.Models
{
    /// <summary>
    /// 担当者状態
    /// </summary>
    public class UserStatus
    {
        /// <summary>
        /// 担当者ID
        /// </summary>
        [Key]
        [MaxLength(13)]
        public string LoginId { get; set; }
        /// <summary>
        /// セッションキー
        /// </summary>
        [MaxLength(36)]
        public string SessionKey { get; set; }
        /// <summary>
        /// デバイス区分
        /// </summary>
        [MaxLength(8)]
        public string DeviceType { get; set; }
        /// <summary>
        /// デバイストークン
        /// </summary>
        [MaxLength(4096)]
        public string DeviceToken { get; set; }
    }
}
