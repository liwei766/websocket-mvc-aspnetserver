using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.Dtos
{
    //ワークスケジュールCSV
    public class WSCsvRow
    {

        //1	作業開始時間 スケジュールに登録された作業開始時間(hh:mm)
        public string Start { get; set; }

        //2	作業名 作業名称（ワークスケジュールに元々あった名称）
        public string Name { get; set; }

        //3	短縮作業名 アプリで使用する作業名称(全角20文字以内)
        public string ShortName { get; set; }

        //4	重要度 作業の重要度(H>M>Lの3段階)
        public string Priority { get; set; }

        //5	作業アイコンID 納品や清掃などアプリでアイコンを表示させるためのID（種類は、11種類）
        public string IconId { get; set; }

        //6	標準作業時間（分）	１作業あたりの標準作業時間
        public String Time { get; set; }

        //7	休日区分 平日と休日を識別する区分（0:平日、1：休日）
        public String Holiday { get; set; }
    }
}
