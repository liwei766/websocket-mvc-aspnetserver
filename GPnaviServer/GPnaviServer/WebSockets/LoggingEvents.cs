using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets
{
    /// <summary>
    /// ロギングイベント
    /// </summary>
    public class LoggingEvents
    {
        #region トレース

        public const int ReceiveAsync = 1001;
        public const int LoginControllerAsync = 1002;
        public const int DownloadRequestControllerAsync = 1003;
        public const int LogoutControllerAsync = 1004;
        public const int RegisterControllerAsync = 1005;
        public const int HelpRequestControllerAsync = 1006;
        public const int ListRequestControllerAsync = 1007;
        public const int Connect = 1008;
        public const int Disconnect = 1009;
        public const int SendMessageAsync = 1010;
        public const int PushMessageAsync = 1011;
        public const int IotHubReceive = 1012;

        #endregion トレース

        #region エラー

        /// <summary>
        /// 例外
        /// </summary>
        public const int Exception = 1100;
        /// <summary>
        /// 装置間IFフォーマット不正
        /// </summary>
        public const int ApiFormat = 1101;
        /// <summary>
        /// 装置間IFバリデーションエラー
        /// </summary>
        public const int Validation = 1102;
        /// <summary>
        /// セッションキー不一致
        /// </summary>
        public const int Session = 1103;
        /// <summary>
        /// マスターが存在しない
        /// </summary>
        public const int Master = 1104;

        #endregion エラー
    }
}
