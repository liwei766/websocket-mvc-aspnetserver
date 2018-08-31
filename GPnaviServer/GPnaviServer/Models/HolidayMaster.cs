using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.Models
{
    /// <summary>
    /// 祝日マスタ
    /// </summary>
    public class HolidayMaster
    {
        /// <summary>
        /// 祝日
        /// </summary>
        [Key]
        public DateTime Holiday { get; set; }
    }
}
