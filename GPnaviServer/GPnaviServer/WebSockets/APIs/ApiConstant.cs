namespace GPnaviServer.WebSockets.APIs
{
    /// <summary>
    /// APIで使用する固定値
    /// </summary>
    public class ApiConstant
    {
        #region メッセージ名
        /// <summary>
        /// ログイン
        /// </summary>
        public const string MESSAGE_LOGIN = "LOGIN";
        /// <summary>
        /// 認証結果
        /// </summary>
        public const string MESSAGE_LOGIN_RESULT = "LOGIN_RESULT";
        /// <summary>
        /// WSマスタバージョン
        /// </summary>
        public const string MESSAGE_WS_VERSION = "WS_VERSION";
        /// <summary>
        /// 担当者マスタバージョン
        /// </summary>
        public const string MESSAGE_MEMBER_VERSION = "MEMBER_VERSION";
        /// <summary>
        /// マスタダウンロード要求
        /// </summary>
        public const string MESSAGE_DOWNLOAD_REQUEST = "DOWNLOAD_REQUEST";
        /// <summary>
        /// WSマスタダウンロード
        /// </summary>
        public const string MESSAGE_WS_DOWNLOAD = "WS_DOWNLOAD";
        /// <summary>
        /// 担当者マスタダウンロード
        /// </summary>
        public const string MESSAGE_MEMBER_DOWNLOAD = "MEMBER_DOWNLOAD";
        /// <summary>
        /// 祝日マスタダウンロード
        /// </summary>
        public const string MESSAGE_HOLIDAY_DOWNLOAD = "HOLIDAY_DOWNLOAD";
        /// <summary>
        /// ログアウト
        /// </summary>
        public const string MESSAGE_LOGOUT = "LOGOUT";
        /// <summary>
        /// ログアウト結果
        /// </summary>
        public const string MESSAGE_LOGOUT_RESULT = "LOGOUT_RESULT";
        /// <summary>
        /// 作業状況登録
        /// </summary>
        public const string MESSAGE_REGISTER = "REGISTER";
        /// <summary>
        /// 登録結果
        /// </summary>
        public const string MESSAGE_REGISTER_RESULT = "REGISTER_RESULT";
        /// <summary>
        /// 作業ステータス
        /// </summary>
        public const string MESSAGE_WORK_STATUS = "WORK_STATUS";
        /// <summary>
        /// レジFF応援（要求）
        /// </summary>
        public const string MESSAGE_HELP_REQUEST = "HELP_REQUEST";
        /// <summary>
        /// レジFF応援（通知）
        /// </summary>
        public const string MESSAGE_HELP_PUSH = "HELP_PUSH";
        /// <summary>
        /// 作業状況一覧作成
        /// </summary>
        public const string MESSAGE_LIST_REQUEST = "LIST_REQUEST";
        /// <summary>
        /// 作業状況一覧
        /// </summary>
        public const string MESSAGE_LIST_RESULT = "LIST_RESULT";
        /// <summary>
        /// センサー検知
        /// </summary>
        public const string MESSAGE_SENSOR_PUSH = "SENSOR_PUSH";
        /// <summary>
        /// センサー検知イベント
        /// </summary>
        public const string MESSAGE_SENSOR_EVENT = "EVENT";
        #endregion メッセージ名

        #region 各項目の選択肢
        /// <summary>
        /// デバイス区分 Android
        /// </summary>
        public const string DEVICE_TYPE_ANDROID = "ANDROID";
        /// <summary>
        /// デバイス区分 Windows IoT Core
        /// </summary>
        public const string DEVICE_TYPE_IOT = "IOT";

        /// <summary>
        /// 認証／登録／作成 結果OK
        /// </summary>
        public const string RESULT_OK = "OK";
        /// <summary>
        /// 認証／登録／作成 結果NG
        /// </summary>
        public const string RESULT_NG = "NG";

        /// <summary>
        /// センサー区分 ポット
        /// </summary>
        public const string SENSOR_TYPE_POT = "POT";
        /// <summary>
        /// センサー区分 ごみ箱
        /// </summary>
        public const string SENSOR_TYPE_TRASH = "TRASH";

        /// <summary>
        /// 作業状態 開始
        /// </summary>
        public const string WORK_STATUS_START = "開始";
        /// <summary>
        /// 作業状態 完了
        /// </summary>
        public const string WORK_STATUS_FINISH = "完了";
        /// <summary>
        /// 作業状態 キャンセル
        /// </summary>
        public const string WORK_STATUS_CANCEL = "キャンセル";
        /// <summary>
        /// 作業状態 一時停止
        /// </summary>
        public const string WORK_STATUS_PAUSE = "一時停止";

        /// <summary>
        /// 作業種別 WS作業
        /// </summary>
        public const string WORK_TYPE_WS = "WS";
        /// <summary>
        /// 作業種別 レジFF作業
        /// </summary>
        public const string WORK_TYPE_RF = "RF";
        /// <summary>
        /// 作業種別 突発（センサー）作業
        /// </summary>
        public const string WORK_TYPE_SE = "SE";

        /// <summary>
        /// レジFF種別 レジ
        /// </summary>
        public const string RF_TYPE_REG = "REG";
        /// <summary>
        /// レジFF種別 FF
        /// </summary>
        public const string RF_TYPE_FF = "FF";

        /// <summary>
        /// マスタ種別 WSマスタ
        /// </summary>
        public const string MASTER_TYPE_WS = "WS";
        /// <summary>
        /// マスタ種別 担当者マスタ
        /// </summary>
        public const string MASTER_TYPE_MEMBER = "MEMBER";

        /// <summary>
        /// 権限 監理者
        /// </summary>
        public const string ROLE_ADMIN = "1";
        /// <summary>
        /// 権限 作業者
        /// </summary>
        public const string ROLE_WORK = "0";

        /// <summary>
        /// 休日区分 休日
        /// </summary>
        public const string HOLIDAY_TRUE = "1";
        /// <summary>
        /// 休日区分 平日
        /// </summary>
        public const string HOLIDAY_FALSE = "0";

        /// <summary>
        /// 応援フラグ OFF
        /// </summary>
        public const string HELP_OFF = "0";
        /// <summary>
        /// 応援フラグ ON
        /// </summary>
        public const string HELP_ON = "1";
        #endregion 各項目の選択肢

        #region ユーザマスタ
        /// <summary>
        /// 担当者ID
        /// </summary>
        public const string LOGINID_JP = "担当者ID";
        /// <summary>
        /// ユーザーIDの最大文字数
        /// </summary>
        public const int LOGINID_LENGTH_MAX = 3;
        /// <summary>
        /// 担当者名
        /// </summary>
        public const string LOGINNAME_JP = "担当者名";
        /// <summary>
        /// ユーザー名の最大文字数
        /// </summary>
        public const int LOGINNAME_LENGTH_MAX = 16;
        /// <summary>
        /// パスワード
        /// </summary>
        public const string PASSWORD_JP = "パスワード";
        /// <summary>
        /// パースワード文字数
        /// </summary>
        public const int PASSWORD_LENGTH_MAX = 4;
        #endregion ユーザマスタ

        #region ワークスケジュールマスタ
        /// <summary>
        /// 作業開始時間
        /// </summary>
        public const string START_JP = "作業開始時間";
        /// <summary>
        /// スタート時刻の桁数の最小限
        /// </summary>
        public const  int START_LENGTH_MIN = 4;
        /// <summary>
        /// 作業名
        /// </summary>
        public const string WS_NAME_JP = "作業名";
        /// <summary>
        /// 作業名の桁数
        /// </summary>
        public const int WS_NAME_LENGTH_MAX = 40;
        /// <summary>
        /// 短縮作業名
        /// </summary>
        public const string WS_SHORTNAME_JP = "短縮作業名";
        /// <summary>
        /// 短縮作業名の桁数
        /// </summary>
        public const int WS_SHORTNAME_LENGTH_MAX = 20;
        /// <summary>
        /// 重要度
        /// </summary>
        public const string PRIORITY_JP = "重要度";
        /// <summary>
        /// 作業アイコンID
        /// </summary>
        public const string ICONID_JP = "作業アイコンID";
        /// <summary>
        /// 標準作業時間（分）
        /// </summary>
        public const string WORK_TIME_JP = "標準作業時間（分）";
        /// <summary>
        /// 休日区分
        /// </summary>
        public const string HOLIDAY_JP = "休日区分";  
        #endregion ワークスケジュールマスタ


        #region 処理インフォメーション
        public const string INFO_UPLOAD_WS_01 = "WS登録OK:合計{0}件追加済";
        public const string INFO_UPLOAD_USER_01 = "ユーザ登録OK:合計{0}件追加済";
        #endregion 処理インフォメーション


        #region エラーメッセージ
        public const string ERR01 = "ログイン名またはパスワードが間違っています。";
        public const string ERR02 = "ログイン名またはパスワードが間違っています。";
        public const string ERR03 = "ログインしなおしてください。";
        public const string ERR04 = "ログインしなおしてください。";
        public const string ERR05 = "作業マスタが更新されたのでログインしなおしてください。";
        public const string ERR06 = "他の担当者が作業中です。";
        public const string ERR07 = "WSマスタがありません。";
        public const string ERR08 = "管理者ではありません。";
        public const string ERR09 = "他の端末がログインしています。";
        public const string ERR10 = "{0}行目の{1}がありません。";
        public const string ERR11 = "{0}行目の{1}は時間の形式ではありません。";
        public const string ERR12 = "{0}行目の{1}の文字数が{2}をこえています。";
        public const string ERR13 = "{0}行目の{1}は不正な値が使用されています。";
        public const string ERR14 = "{0}行目の{1}は半角英数字を使用してください。";
        public const string ERR15 = "{0}行目の{1}が重複しています。";
        public const string ERR90 = "システムエラーです。";
        #endregion エラーメッセージ
    }
}
