using GPnaviServer.Data;
using GPnaviServer.Models;
using GPnaviServer.Utilities;
using GPnaviServer.WebSockets.APIs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets
{
    /// <summary>
    /// WebSocket API ハンドラ
    /// </summary>
    public class WebSocketApiHandler : WebSocketHandler
    {
        /// <summary>
        /// 作業履歴で使用するレジFF作業の表示名 レジ
        /// </summary>
        private const string DISPLAY_NAME_REG = "レジ";
        /// <summary>
        /// 作業履歴で使用するレジFF作業の表示名 FF
        /// </summary>
        private const string DISPLAY_NAME_FF = "ＦＦ";
        /// <summary>
        /// WS作業状態の有効期限（単位：時間）
        /// </summary>
        private const int WS_STATUS_LIMIT = 12;

        /// <summary>
        /// DateTimeを文字列化するときのフォーマットプロバイダ
        /// </summary>
        public CultureInfo CultureInfoApi => CultureInfo.CreateSpecificCulture("ja-JP");
        /// <summary>
        /// 装置間IFの日付時刻フォーマット
        /// </summary>
        public string DateTimeFormat => @"yyyy/MM/dd HH:mm:ss.fff";
        /// <summary>
        /// 祝日テーブルの祝日フォーマット
        /// </summary>
        public string HolidayFormat => @"yyyy/MM/dd";
        /// <summary>
        /// 設定
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="webSocketConnectionManager">マネージャ</param>
        /// <param name="logger">ロガー</param>
        /// <param name="configuration">設定</param>
        public WebSocketApiHandler(WebSocketConnectionManager webSocketConnectionManager, ILogger<WebSocketApiHandler> logger, IConfiguration configuration) : base(webSocketConnectionManager, logger)
        {
            Configuration = configuration;
        }

        public override async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            try
            {
                // 受信メッセージ取得
                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                _logger.LogInformation(LoggingEvents.ReceiveAsync, $"-- RECV -- {json}");

                _logger.LogTrace(LoggingEvents.ReceiveAsync, "パース");
                var apiCommonUp = JsonConvert.DeserializeObject<ApiCommonUp>(json);
                if (apiCommonUp != null && !string.IsNullOrEmpty(apiCommonUp.message_name))
                {
                    _logger.LogTrace(LoggingEvents.ReceiveAsync, "受信メッセージ名で分岐");
                    switch (apiCommonUp.message_name)
                    {
                        case ApiConstant.MESSAGE_LOGIN:
                            await LoginControllerAsync(socket, json);
                            break;

                        case ApiConstant.MESSAGE_DOWNLOAD_REQUEST:
                            await DownloadRequestControllerAsync(socket, json);
                            break;

                        case ApiConstant.MESSAGE_LOGOUT:
                            await LogoutControllerAsync(socket, json);
                            break;

                        case ApiConstant.MESSAGE_REGISTER:
                            await RegisterControllerAsync(socket, json);
                            break;

                        case ApiConstant.MESSAGE_HELP_REQUEST:
                            await HelpRequestControllerAsync(socket, json);
                            break;

                        case ApiConstant.MESSAGE_LIST_REQUEST:
                            await ListRequestController(socket, json);
                            break;

                        default:
                            _logger.LogError(LoggingEvents.ApiFormat, $"装置間IFのメッセージ名 {apiCommonUp.message_name} は存在しません。");
                            break;
                    }
                }
                else
                {
                    _logger.LogError(LoggingEvents.ApiFormat, $"装置間IFのメッセージ名がありません。");
                }
            }
            catch (JsonReaderException ex)
            {
                _logger.LogError(LoggingEvents.ApiFormat, $"装置間IFフォーマットではありません。{ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(LoggingEvents.Exception, ex, "ReceiveAsync unknown error.");
            }
        }
        /// <summary>
        /// ログインコントローラ
        /// </summary>
        /// <param name="socket">要求元</param>
        /// <param name="json">受信メッセージ</param>
        /// <returns></returns>
        private async Task LoginControllerAsync(WebSocket socket, string json)
        {
            _logger.LogInformation(LoggingEvents.LoginControllerAsync, "LoginControllerAsync START");

            var options = new DbContextOptionsBuilder<GPnaviServerContext>()
                .UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
                .Options;
            using (var context = new GPnaviServerContext(options))
            {
                try
                {
                    _logger.LogTrace(LoggingEvents.LoginControllerAsync, "パース");
                    var apiLogin = JsonConvert.DeserializeObject<ApiLogin>(json);

                    _logger.LogTrace(LoggingEvents.LoginControllerAsync, "バリデーションチェック");
                    var (result, message) = IsInvalidApiLogin(apiLogin);
                    if (result)
                    {
                        _logger.LogError(LoggingEvents.Validation, $"バリデーションエラー {message}");
                        await SendLoginResultNGAsync(socket, nameof(ApiConstant.ERR90), ApiConstant.ERR90);
                        return;
                    }

                    _logger.LogTrace(LoggingEvents.LoginControllerAsync, "担当者マスタ検索");
                    // #84 IsValidをAND条件に追加
                    var userMaster = context.UserMasters.FirstOrDefault(e => e.LoginId.Equals(apiLogin.login_id) && e.IsValid);
                    if (userMaster == null)
                    {
                        // 担当者マスタに存在しない
                        _logger.LogError(LoggingEvents.Validation, $"担当者マスタに存在しない login_id={apiLogin.login_id}");
                        await SendLoginResultNGAsync(socket, nameof(ApiConstant.ERR01), ApiConstant.ERR01);
                        return;
                    }

                    _logger.LogTrace(LoggingEvents.LoginControllerAsync, "デバイス区分判定");
                    if (apiLogin.device_type.Equals(ApiConstant.DEVICE_TYPE_ANDROID))
                    {
                        // 店員用デバイス端末の場合のみパスワードチェックを行う
                        _logger.LogTrace(LoggingEvents.LoginControllerAsync, "パスワードチェック");
                        var passwordHash = PasswordUtility.Hash(apiLogin.password);
                        if (!userMaster.Password.Equals(passwordHash))
                        {
                            // パスワード不一致
                            _logger.LogError(LoggingEvents.Validation, $"パスワード不一致 login_id={apiLogin.login_id}");
                            await SendLoginResultNGAsync(socket, nameof(ApiConstant.ERR02), ApiConstant.ERR02);
                            return;
                        }
                    }

                    _logger.LogTrace(LoggingEvents.LoginControllerAsync, "担当者ステータス検索");
                    var userStatus = context.UserStatuses.FirstOrDefault(e => e.LoginId.Equals(apiLogin.login_id));
                    if (userStatus != null && !string.IsNullOrEmpty(userStatus.SessionKey))
                    {
                        // ログイン中

                        try
                        {
                            _logger.LogInformation(LoggingEvents.LoginControllerAsync, "解放処理の失敗を許容するようにスコープを作る");
                            _logger.LogTrace(LoggingEvents.LoginControllerAsync, "WS作業状態を検索");
                            var workScheduleStatus = context.WorkScheduleStatuses.FirstOrDefault(e => e.LoginId.Equals(userMaster.LoginId));
                            if (workScheduleStatus != null)
                            {
                                _logger.LogTrace(LoggingEvents.LoginControllerAsync, "作業状態をチェック");
                                if (!(workScheduleStatus.Status.Equals(ApiConstant.WORK_STATUS_CANCEL) || workScheduleStatus.Status.Equals(ApiConstant.WORK_STATUS_FINISH)))
                                {
                                    _logger.LogTrace(LoggingEvents.LoginControllerAsync, "キャンセル、完了以外の場合はキャンセルする");
                                    workScheduleStatus.Status = ApiConstant.WORK_STATUS_CANCEL;

                                    _logger.LogTrace(LoggingEvents.LoginControllerAsync, "WSマスタを検索する（短縮名が必要）");
                                    var workScheduleMaster = context.WorkScheduleMasters.FirstOrDefault(e =>
                                         e.Version.Equals(workScheduleStatus.Version) &&
                                         e.Start.Equals(workScheduleStatus.Start) &&
                                         e.Name.Equals(workScheduleStatus.Name) &&
                                         e.Holiday.Equals(workScheduleStatus.Holiday)
                                    );

                                    _logger.LogTrace(LoggingEvents.LoginControllerAsync, "作業状況履歴を追加する");
                                    var workStatusHistory = new WorkStatusHistory
                                    {
                                        Version = workScheduleStatus.Version,
                                        Start = workScheduleStatus.Start,
                                        Name = workScheduleStatus.Name,
                                        Holiday = workScheduleStatus.Holiday,
                                        RfType = "",
                                        SensorId = "",
                                        DisplayName = workScheduleMaster?.ShortName,
                                        LoginId = userMaster.LoginId,
                                        LoginName = userMaster.LoginName,
                                        Status = workScheduleStatus.Status,
                                        StartDate = workScheduleStatus.StartDate,
                                        RegisterDate = DateTime.Now
                                    };
                                    if (!IsExistPrimaryKeyInWorkStatusHistory(context, workStatusHistory))
                                    {
                                        context.WorkStatusHistories.Add(workStatusHistory);
                                    }

                                    // 保存を一か所にすることで例外回避するか様子を見る
                                    //_logger.LogTrace(LoggingEvents.LoginControllerAsync, "保存する");
                                    //await _context.SaveChangesAsync();

                                    _logger.LogTrace(LoggingEvents.LoginControllerAsync, "作業ステータスを送信する");
                                    var apiWorkStatus = new ApiWorkStatus
                                    {
                                        message_name = ApiConstant.MESSAGE_WORK_STATUS,
                                        work_status = workScheduleStatus.Status,
                                        ws_version = workScheduleStatus.Version.ToString(),
                                        ws_start = workScheduleStatus.Start,
                                        ws_name = workScheduleStatus.Name,
                                        ws_holiday = workScheduleStatus.Holiday,
                                        start_date = workScheduleStatus.StartDate.ToString(DateTimeFormat, CultureInfoApi)
                                    };
                                    var workStatusJson = JsonConvert.SerializeObject(apiWorkStatus);
                                    await SendWorkStatusMessageToAllAsync(context, workStatusJson);
                                }
                            }

                            _logger.LogTrace(LoggingEvents.LoginControllerAsync, "突発作業状態を検索");
                            var sensorStatus = context.SensorStatuses.FirstOrDefault(e => e.LoginId.Equals(userMaster.LoginId));
                            if (sensorStatus != null)
                            {
                                _logger.LogTrace(LoggingEvents.LoginControllerAsync, "作業状態をチェック");
                                if (!(sensorStatus.Status.Equals(ApiConstant.WORK_STATUS_CANCEL) || sensorStatus.Status.Equals(ApiConstant.WORK_STATUS_FINISH)))
                                {
                                    _logger.LogTrace(LoggingEvents.LoginControllerAsync, "キャンセル、完了以外の場合はキャンセルする");
                                    sensorStatus.Status = ApiConstant.WORK_STATUS_CANCEL;

                                    _logger.LogTrace(LoggingEvents.LoginControllerAsync, "作業状況履歴を追加する");
                                    var workStatusHistory = new WorkStatusHistory
                                    {
                                        Version = 0,
                                        Start = "",
                                        Name = "",
                                        Holiday = "",
                                        RfType = "",
                                        SensorId = sensorStatus.SensorId,
                                        DisplayName = sensorStatus.DisplayName,
                                        LoginId = userMaster.LoginId,
                                        LoginName = userMaster.LoginName,
                                        Status = sensorStatus.Status,
                                        StartDate = sensorStatus.StartDate,
                                        RegisterDate = DateTime.Now
                                    };
                                    if (!IsExistPrimaryKeyInWorkStatusHistory(context, workStatusHistory))
                                    {
                                        context.WorkStatusHistories.Add(workStatusHistory);
                                    }

                                    // 保存を一か所にすることで例外回避するか様子を見る
                                    //_logger.LogTrace(LoggingEvents.LoginControllerAsync, "保存する");
                                    //await _context.SaveChangesAsync();

                                    _logger.LogTrace(LoggingEvents.LoginControllerAsync, "作業ステータスを送信する");
                                    var apiWorkStatus = new ApiWorkStatus
                                    {
                                        message_name = ApiConstant.MESSAGE_WORK_STATUS,
                                        work_status = sensorStatus.Status,
                                        sensor_id = sensorStatus.SensorId,
                                        start_date = sensorStatus.StartDate.ToString(DateTimeFormat, CultureInfoApi)
                                    };
                                    var workStatusJson = JsonConvert.SerializeObject(apiWorkStatus);
                                    await SendWorkStatusMessageToAllAsync(context, workStatusJson);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(LoggingEvents.Exception, ex, "LoginControllerAsync 解放中にunknown error.");
                        }
                    }

                    _logger.LogTrace(LoggingEvents.LoginControllerAsync, "セッションキー生成");
                    var sessionKey = Guid.NewGuid().ToString();
                    if (userStatus == null)
                    {
                        _logger.LogTrace(LoggingEvents.LoginControllerAsync, "担当者ステータス追加");
                        userStatus = new UserStatus
                        {
                            LoginId = apiLogin.login_id,
                            SessionKey = sessionKey,
                            DeviceType = apiLogin.device_type,
                            DeviceToken = apiLogin.device_token
                        };
                        context.Add(userStatus);
                    }
                    else
                    {
                        _logger.LogTrace(LoggingEvents.LoginControllerAsync, "担当者ステータス更新");
                        userStatus.SessionKey = sessionKey;
                        userStatus.DeviceType = apiLogin.device_type;
                        userStatus.DeviceToken = apiLogin.device_token;
                    }

                    _logger.LogTrace(LoggingEvents.LoginControllerAsync, "セッションキーテーブルを更新する");
                    var sessionInformation = WebSocketConnectionManager.GetSessionInformation(socket);
                    sessionInformation.LoginId = apiLogin.login_id;
                    sessionInformation.SessionKey = sessionKey;

                    _logger.LogTrace(LoggingEvents.LoginControllerAsync, "保存する");
                    await context.SaveChangesAsync();

                    _logger.LogTrace(LoggingEvents.LoginControllerAsync, "認証OKを送信する");
                    var apiLoginResult = new ApiLoginResult
                    {
                        message_name = ApiConstant.MESSAGE_LOGIN_RESULT,
                        result = ApiConstant.RESULT_OK,
                        session_key = sessionKey,
                        role = userMaster.Role
                    };
                    var resultJson = JsonConvert.SerializeObject(apiLoginResult);
                    await SendMessageAsync(socket, resultJson);

                    _logger.LogTrace(LoggingEvents.LoginControllerAsync, "WSマスタバージョンを送信する");
                    if (context.WorkScheduleVersions.Count() > 0)
                    {
                        var wsVersion = context.WorkScheduleVersions.Select(e => e.Id).Max();
                        await SendWsVersionAsync(socket, wsVersion.ToString());
                    }
                    else
                    {
                        // WSマスタを登録されたことがない
                        _logger.LogError(LoggingEvents.Master, "WSマスタを登録されたことがない");
                        await SendWsVersionAsync(socket, "0");
                    }

                    // 担当者マスタバージョンを送信する
                    if (context.UserVersions.Count() > 0)
                    {
                        var userVersion = context.UserVersions.Select(e => e.Id).Max();
                        await SendMemberVersionAsync(socket, userVersion.ToString());
                    }
                    else
                    {
                        // 担当者マスタを登録されたことがない
                        _logger.LogError(LoggingEvents.Master, "担当者マスタを登録されたことがない");
                        await SendMemberVersionAsync(socket, "0");
                    }

                }
                catch (JsonWriterException ex)
                {
                    _logger.LogError(LoggingEvents.ApiFormat, $"装置間IFフォーマットに変換できませんでした。{ex.Message}");
                    await SendLoginResultNGAsync(socket, nameof(ApiConstant.ERR90), ApiConstant.ERR90);
                }
                catch (JsonReaderException ex)
                {
                    _logger.LogError(LoggingEvents.ApiFormat, $"装置間IFフォーマットではありません。{ex.Message}");
                    await SendLoginResultNGAsync(socket, nameof(ApiConstant.ERR90), ApiConstant.ERR90);
                }
                catch (Exception ex)
                {
                    _logger.LogError(LoggingEvents.Exception, ex, $"LoginControllerAsync unknown error. {json}");
                    await SendLoginResultNGAsync(socket, nameof(ApiConstant.ERR90), ApiConstant.ERR90);
                }
            }
        }
        /// <summary>
        /// 作業状況履歴重複判定
        /// </summary>
        /// <param name="context">DBコンテキスト</param>
        /// <param name="workStatusHistory">作業状況履歴アイテム</param>
        /// <returns>プライマリキーが存在している場合は真</returns>
        private bool IsExistPrimaryKeyInWorkStatusHistory(GPnaviServerContext context, WorkStatusHistory workStatusHistory)
        {
            try
            {
                var result = context.WorkStatusHistories.Any(e =>
                    e.Version == workStatusHistory.Version &&
                    e.Start.Equals(workStatusHistory.Start) &&
                    e.Name.Equals(workStatusHistory.Name) &&
                    e.Holiday.Equals(workStatusHistory.Holiday) &&
                    e.SensorId.Equals(workStatusHistory.SensorId) &&
                    e.RegisterDate.Equals(workStatusHistory.RegisterDate)
                );

                if (result)
                {
                    _logger.LogError(LoggingEvents.LoginControllerAsync, $"履歴重複" +
                        $" ver={workStatusHistory.Version}" +
                        $" sta={workStatusHistory.Start}" +
                        $" nam={workStatusHistory.Name}" +
                        $" hol={workStatusHistory.Holiday}" +
                        $" sid={workStatusHistory.SensorId}" +
                        $" rdt={workStatusHistory.RegisterDate}" +
                        $" dpn={workStatusHistory.DisplayName}" +
                        $" sts={workStatusHistory.Status}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(LoggingEvents.LoginControllerAsync, ex, $"履歴重複チェック中に例外" +
                    $" ver={workStatusHistory.Version}" +
                    $" sta={workStatusHistory.Start}" +
                    $" nam={workStatusHistory.Name}" +
                    $" hol={workStatusHistory.Holiday}" +
                    $" sid={workStatusHistory.SensorId}" +
                    $" rdt={workStatusHistory.RegisterDate}" +
                    $" dpn={workStatusHistory.DisplayName}" +
                    $" sts={workStatusHistory.Status}");

                // 例外発生しているので登録させない
                return true;
            }
        }

        /// <summary>
        /// 作業ステータスを送信する
        /// </summary>
        /// <param name="context">DBコンテキスト</param>
        /// <param name="workStatusJson">送信メッセージ</param>
        /// <returns></returns>
        private async Task SendWorkStatusMessageToAllAsync(GPnaviServerContext context, string workStatusJson)
        {
            try
            {
#if WORK_STATUS_ON_WEBSOCKET
                await SendMessageToAllAsync(workStatusJson);
#else
                await PushMessageToAllAsync(context, workStatusJson);
#endif
                _logger.LogInformation(LoggingEvents.PushMessageAsync, $"作業ステータスの送信に成功しました。{workStatusJson}");
            }
            catch (Exception ex)
            {
                // 作業ステータスの送信後にNGを返すシーケンスはないのでここで終端する
                _logger.LogError(LoggingEvents.Exception, ex, $"作業ステータスの送信に失敗しました。{workStatusJson}");
            }
        }

        /// <summary>
        /// WSマスタバージョンを送信する
        /// </summary>
        /// <param name="socket">要求元</param>
        /// <param name="version">バージョン</param>
        /// <returns></returns>
        private async Task SendWsVersionAsync(WebSocket socket, string version)
        {
            try
            {
                var apiWsVersion = new ApiWsVersion
                {
                    message_name = ApiConstant.MESSAGE_WS_VERSION,
                    version = version
                };
                var json = JsonConvert.SerializeObject(apiWsVersion);
                await SendMessageAsync(socket, json);
            }
            catch (Exception ex)
            {
                // 送信後にNGを返すシーケンスはないのでここで終端する
                _logger.LogError(LoggingEvents.Exception, ex, $"WSマスタバージョンの送信に失敗しました。{version}");
            }
        }

        /// <summary>
        /// 担当者マスタバージョンを送信する
        /// </summary>
        /// <param name="socket">要求元</param>
        /// <param name="version">バージョン</param>
        /// <returns></returns>
        private async Task SendMemberVersionAsync(WebSocket socket, string version)
        {
            try
            {
                var apiMemberVersion = new ApiMemberVersion
                {
                    message_name = ApiConstant.MESSAGE_MEMBER_VERSION,
                    version = version
                };
                var json = JsonConvert.SerializeObject(apiMemberVersion);
                await SendMessageAsync(socket, json);
            }
            catch (Exception ex)
            {
                // 送信後にNGを返すシーケンスはないのでここで終端する
                _logger.LogError(LoggingEvents.Exception, ex, $"担当者マスタバージョンの送信に失敗しました。{version}");
            }
        }

        /// <summary>
        /// 認証結果NGを送信する
        /// </summary>
        /// <param name="socket">要求元</param>
        /// <param name="code">エラーコード</param>
        /// <param name="message">エラーメッセージ</param>
        /// <returns></returns>
        private async Task SendLoginResultNGAsync(WebSocket socket, string code, string message)
        {
            try
            {
                var apiLoginResult = new ApiLoginResult
                {
                    message_name = ApiConstant.MESSAGE_LOGIN_RESULT,
                    result = ApiConstant.RESULT_NG,
                    error_code = code,
                    error_message = message
                };
                var json = JsonConvert.SerializeObject(apiLoginResult);
                await SendMessageAsync(socket, json);
            }
            catch (Exception ex)
            {
                // 送信後にNGを返すシーケンスはないのでここで終端する
                _logger.LogError(LoggingEvents.Exception, ex, $"認証結果NGの送信に失敗しました。{code} {message}");
            }
        }

        /// <summary>
        /// 受信メッセージバリデーション セッションキー以外の共通部.
        /// ログイン時に使用する.
        /// </summary>
        /// <param name="apiCommonUp">受信メッセージ</param>
        /// <returns>エラーの場合は真</returns>
        private (bool result, string message) IsInvalidApiCommonUpWithoutSessionKey(ApiCommonUp apiCommonUp)
        {
            if (apiCommonUp == null)
            {
                return (true, "apiCommonUp is null.");
            }

            // message_nameはReceiveAsyncでチェック済みなのでここでは不要

            if (string.IsNullOrWhiteSpace(apiCommonUp.device_type))
            {
                return (true, "device_type is null.");
            }

            if (!(apiCommonUp.device_type.Equals(ApiConstant.DEVICE_TYPE_ANDROID) || apiCommonUp.device_type.Equals(ApiConstant.DEVICE_TYPE_IOT)))
            {
                return (true, $"device_type {apiCommonUp.device_type} is invalid.");
            }

            if (string.IsNullOrWhiteSpace(apiCommonUp.login_id))
            {
                return (true, "login_id is null.");
            }

            return (false, null);
        }
        /// <summary>
        /// 受信メッセージバリデーション セッションキーを含む共通部
        /// </summary>
        /// <param name="apiCommonUp"></param>
        /// <returns></returns>
        private (bool result, string message) IsInvalidApiCommonUp(ApiCommonUp apiCommonUp)
        {
            var (result, message) = IsInvalidApiCommonUpWithoutSessionKey(apiCommonUp);
            if (result)
            {
                return (result, message);
            }

            if (string.IsNullOrWhiteSpace(apiCommonUp.session_key))
            {
                return (true, "session_key is null.");
            }
            return (false, null);
        }
        /// <summary>
        /// 受信メッセージバリデーション ログイン
        /// </summary>
        /// <param name="apiLogin">受信メッセージ</param>
        /// <returns>バリデーションエラーの場合は真</returns>
        private (bool result, string message) IsInvalidApiLogin(ApiLogin apiLogin)
        {
            if (apiLogin == null)
            {
                return (true, "apiLogin is null.");
            }

            var (result, message) = IsInvalidApiCommonUpWithoutSessionKey(apiLogin);
            if (result)
            {
                return (result, message);
            }

            if (string.IsNullOrWhiteSpace(apiLogin.password) && apiLogin.device_type.Equals(ApiConstant.DEVICE_TYPE_ANDROID))
            {
                return (true, "password is null.");
            }

            if (string.IsNullOrWhiteSpace(apiLogin.device_token))
            {
                return (true, "device_token is null.");
            }

            return (false, null);
        }
        /// <summary>
        /// マスタダウンロード要求コントローラ
        /// </summary>
        /// <param name="socket">要求元</param>
        /// <param name="json">受信メッセージ</param>
        /// <returns></returns>
        private async Task DownloadRequestControllerAsync(WebSocket socket, string json)
        {
            _logger.LogInformation(LoggingEvents.DownloadRequestControllerAsync, "DownloadRequestControllerAsync START");

            var options = new DbContextOptionsBuilder<GPnaviServerContext>()
                .UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
                .Options;
            using (var context = new GPnaviServerContext(options))
            {
                try
                {
                    _logger.LogTrace(LoggingEvents.DownloadRequestControllerAsync, "パース");
                    var apiDownloadRequest = JsonConvert.DeserializeObject<ApiDownloadRequest>(json);

                    _logger.LogTrace(LoggingEvents.DownloadRequestControllerAsync, "バリデーションチェック");
                    var (result, message) = IsInvalidApiDownloadRequest(apiDownloadRequest);
                    if (result)
                    {
                        _logger.LogError(LoggingEvents.Validation, $"バリデーションエラー {message}");
                        return;
                    }

                    _logger.LogTrace(LoggingEvents.DownloadRequestControllerAsync, "担当者ステータス検索");
                    var userStatus = context.UserStatuses.FirstOrDefault(e => e.LoginId.Equals(apiDownloadRequest.login_id));

                    _logger.LogTrace(LoggingEvents.DownloadRequestControllerAsync, "セッションキーチェック");
                    if (userStatus == null || !apiDownloadRequest.session_key.Equals(userStatus.SessionKey))
                    {
                        // セッションキー不一致
                        _logger.LogError(LoggingEvents.Session, $"セッションキー不一致 api.login_id={apiDownloadRequest.login_id} api.session_key={apiDownloadRequest.session_key} userStatus.SessionKey={userStatus?.SessionKey}");
                        return;
                    }

                    _logger.LogTrace(LoggingEvents.DownloadRequestControllerAsync, "セッションキーテーブルを更新する");
                    UpdateSessionInformation(socket, apiDownloadRequest);

                    _logger.LogTrace(LoggingEvents.DownloadRequestControllerAsync, "種別チェック");
                    if (apiDownloadRequest.type == ApiConstant.MASTER_TYPE_WS)
                    {
                        // WSマスタ
                        if (context.WorkScheduleVersions.Count() > 0)
                        {
                            _logger.LogTrace(LoggingEvents.DownloadRequestControllerAsync, "WSダウンロードメッセージを作成する");
                            var wsVersion = context.WorkScheduleVersions.Select(e => e.Id).Max();
                            var apiWsDownload = new ApiWsDownload
                            {
                                message_name = ApiConstant.MESSAGE_WS_DOWNLOAD,
                                version = wsVersion.ToString()
                            };
                            context.WorkScheduleMasters.Where(e => e.Version == wsVersion).ToList().ForEach(e =>
                            {
                                apiWsDownload.ws_list.Add(new ApiWsDownload.WorkSchedule
                                {
                                    ws_start = e.Start,
                                    ws_name = e.Name,
                                    ws_short_name = e.ShortName,
                                    ws_priority = e.Priority,
                                    ws_icon_id = e.IconId,
                                    ws_time = e.Time,
                                    ws_holiday = e.Holiday,
                                    ws_row = e.Row.ToString()
                                });
                            });

                            _logger.LogTrace(LoggingEvents.DownloadRequestControllerAsync, "WSダウンロードメッセージを送信する");
                            var responseJson = JsonConvert.SerializeObject(apiWsDownload);
                            await SendMessageAsync(socket, responseJson);
                        }
                        else
                        {
                            _logger.LogError(LoggingEvents.Master, "WSマスタバージョンを登録されたことがない");
                        }

                        // 祝日マスタ
                        _logger.LogTrace(LoggingEvents.DownloadRequestControllerAsync, "祝日マスタダウンロードメッセージを作成する");
                        var apiHolidayDownload = new ApiHolidayDownload
                        {
                            message_name = ApiConstant.MESSAGE_HOLIDAY_DOWNLOAD
                        };
                        context.HolidayMasters.ToList().ForEach(e =>
                        {
                            var holidayString = e.Holiday.ToString(HolidayFormat, CultureInfoApi);
                            apiHolidayDownload.holiday_list.Add(holidayString);
                        });

                        _logger.LogTrace(LoggingEvents.DownloadRequestControllerAsync, "祝日マスタダウンロードメッセージを送信する");
                        var holidayJson = JsonConvert.SerializeObject(apiHolidayDownload);
                        await SendMessageAsync(socket, holidayJson);
                    }
                    else
                    {
                        // 担当者マスタ
                        if (context.UserVersions.Count() > 0)
                        {
                            _logger.LogTrace(LoggingEvents.DownloadRequestControllerAsync, "担当者ダウンロードメッセージを作成する");
                            var userVersion = context.UserVersions.Select(e => e.Id).Max();
                            var apiMemberDownload = new ApiMemberDownload
                            {
                                message_name = ApiConstant.MESSAGE_MEMBER_DOWNLOAD,
                                version = userVersion.ToString()
                            };
                            context.UserMasters.Where(e => e.IsValid).ToList().ForEach(e =>
                            {
                                apiMemberDownload.member_list.Add(new ApiMemberDownload.Member
                                {
                                    login_id = e.LoginId,
                                    login_name = e.LoginName
                                });
                            });

                            _logger.LogTrace(LoggingEvents.DownloadRequestControllerAsync, "担当者ダウンロードメッセージを送信する");
                            var responseJson = JsonConvert.SerializeObject(apiMemberDownload);
                            await SendMessageAsync(socket, responseJson);
                        }
                        else
                        {
                            _logger.LogError(LoggingEvents.Master, "担当者マスタバージョンを登録されたことがない");
                        }
                    }

                }
                catch (JsonWriterException ex)
                {
                    _logger.LogError(LoggingEvents.ApiFormat, $"装置間IFフォーマットに変換できませんでした。{ex.Message}");
                }
                catch (JsonReaderException ex)
                {
                    _logger.LogError(LoggingEvents.ApiFormat, $"装置間IFフォーマットではありません。{ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(LoggingEvents.Exception, ex, "LoginControllerAsync unknown error.");
                }
            }
        }
        /// <summary>
        /// セッションキーテーブルを更新する
        /// </summary>
        /// <param name="socket">要求元</param>
        /// <param name="apiCommonUp">受信メッセージ</param>
        private void UpdateSessionInformation(WebSocket socket, ApiCommonUp apiCommonUp)
        {
            var sessionInformation = WebSocketConnectionManager.GetSessionInformation(socket);
            sessionInformation.LoginId = apiCommonUp.login_id;
            sessionInformation.SessionKey = apiCommonUp.session_key;
        }

        /// <summary>
        /// 受信メッセージバリデーション マスタダウンロード要求
        /// </summary>
        /// <param name="apiDownloadRequest">受信メッセージ</param>
        /// <returns>バリデーションエラーの場合は真</returns>
        private (bool result, string message) IsInvalidApiDownloadRequest(ApiDownloadRequest apiDownloadRequest)
        {
            if (apiDownloadRequest == null)
            {
                return (true, "apiDownloadRequest is null.");
            }

            var (result, message) = IsInvalidApiCommonUp(apiDownloadRequest);
            if (result)
            {
                return (result, message);
            }

            if (string.IsNullOrWhiteSpace(apiDownloadRequest.type))
            {
                return (true, "type is null.");
            }

            if (!(apiDownloadRequest.type == ApiConstant.MASTER_TYPE_WS || apiDownloadRequest.type == ApiConstant.MASTER_TYPE_MEMBER))
            {
                return (true, $"type {apiDownloadRequest.type} is invalid.");
            }

            return (false, null);
        }
        /// <summary>
        /// ログアウトコントローラ
        /// </summary>
        /// <param name="socket">要求元</param>
        /// <param name="json">受信メッセージ</param>
        /// <returns></returns>
        private async Task LogoutControllerAsync(WebSocket socket, string json)
        {
            _logger.LogInformation(LoggingEvents.LogoutControllerAsync, "LogoutController START");
            var options = new DbContextOptionsBuilder<GPnaviServerContext>()
                .UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
                .Options;
            using (var context = new GPnaviServerContext(options))
            {
                try
                {
                    _logger.LogTrace(LoggingEvents.LogoutControllerAsync, "パース");
                    var apiLogout = JsonConvert.DeserializeObject<ApiLogout>(json);

                    _logger.LogTrace(LoggingEvents.LogoutControllerAsync, "バリデーションチェック");
                    var (result, message) = IsInvalidApiLogout(apiLogout);
                    if (result)
                    {
                        _logger.LogError(LoggingEvents.Validation, $"バリデーションエラー {message}");
                        return;
                    }

                    _logger.LogTrace(LoggingEvents.LogoutControllerAsync, "担当者ステータス検索");
                    var userStatus = context.UserStatuses.FirstOrDefault(e => e.LoginId.Equals(apiLogout.login_id));
                    if (userStatus == null || string.IsNullOrWhiteSpace(userStatus.SessionKey))
                    {
                        // 担当者（担当者ステータス）に存在しない
                        _logger.LogError(LoggingEvents.Validation, $"セッションキーが存在しない login_id={apiLogout.login_id}");
                        return;
                    }

                    _logger.LogTrace(LoggingEvents.LogoutControllerAsync, "セッションキーリストから削除");
                    var sessionInfo = WebSocketConnectionManager.GetSessionInformation(socket);
                    if (sessionInfo != null)
                    {
                        sessionInfo.LoginId = "";
                        sessionInfo.SessionKey = "";

                        // 端末から切断されるまでソケットは保持することになる
                        // 端末から切断された時はミドルウェアが切断処理を行う
                    }

                    _logger.LogTrace(LoggingEvents.LogoutControllerAsync, "DBの担当者状態テーブルを更新する");
                    userStatus.SessionKey = "";
                    userStatus.DeviceToken = "";
                    userStatus.DeviceType = "";
                    context.SaveChanges();

                }
                catch (JsonWriterException ex)
                {
                    _logger.LogError(LoggingEvents.ApiFormat, $"装置間IFフォーマットに変換できませんでした。{ex.Message}");
                }
                catch (JsonReaderException ex)
                {
                    _logger.LogError(LoggingEvents.ApiFormat, $"装置間IFフォーマットではありません。{ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(LoggingEvents.Exception, ex, "LogoutControllerAsync unknown error.");
                }
                finally
                {
                    // 常にログアウトOKを送信する
                    try
                    {
                        await SendMessageAsync(socket, JsonConvert.SerializeObject(new ApiLogoutResult
                        {
                            message_name = ApiConstant.MESSAGE_LOGOUT_RESULT,
                            result = ApiConstant.RESULT_OK,
                        }));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(LoggingEvents.Exception, ex, "LOGOUT_RESULT send error.");
                    }
                }
            }
        }
        /// <summary>
        /// 受信メッセージバリデーション ログアウト
        /// </summary>
        /// <param name="apiLogout">受信メッセージ</param>
        /// <returns>バリデーションエラーの場合は真</returns>
        private (bool result, string message) IsInvalidApiLogout(ApiLogout apiLogout)
        {
            if (apiLogout == null)
            {
                return (true, "apiLogout is null.");
            }

            if (string.IsNullOrWhiteSpace(apiLogout.login_id))
            {
                return (true, "login_id is null.");
            }

            if (string.IsNullOrWhiteSpace(apiLogout.session_key))
            {
                return (true, "session_key is null.");
            }

            return (false, null);
        }

        /// <summary>
        /// 作業状況登録コントローラ
        /// </summary>
        /// <param name="socket">要求元</param>
        /// <param name="json">受信メッセージ</param>
        /// <returns></returns>
        private async Task RegisterControllerAsync(WebSocket socket, string json)
        {
            _logger.LogInformation(LoggingEvents.RegisterControllerAsync, "RegisterControllerAsync START");

            var options = new DbContextOptionsBuilder<GPnaviServerContext>()
                .UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
                .Options;
            using (var context = new GPnaviServerContext(options))
            {
                try
                {
                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "パース");
                    var apiRegister = JsonConvert.DeserializeObject<ApiRegister>(json);

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "バリデーションチェック");
                    var (result, message) = IsInvalidApiRegister(apiRegister);
                    if (result)
                    {
                        _logger.LogError(LoggingEvents.Validation, $"バリデーションエラー {message}");
                        await SendRegisterResultNGAsync(socket, nameof(ApiConstant.ERR90), ApiConstant.ERR90);
                        return;
                    }

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "担当者ステータス検索");
                    var userStatus = context.UserStatuses.FirstOrDefault(e => e.LoginId.Equals(apiRegister.login_id));

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "セッションキーチェック");
                    if (userStatus == null || string.IsNullOrEmpty(apiRegister.session_key))
                    {
                        _logger.LogError(LoggingEvents.Session, $"セッションキーが無い api.login_id={apiRegister.login_id} api.session_key={apiRegister.session_key} userStatus.SessionKey={userStatus?.SessionKey}");
                        await SendRegisterResultNGAsync(socket, nameof(ApiConstant.ERR03), ApiConstant.ERR03);
                        return;
                    }
                    if (!apiRegister.session_key.Equals(userStatus.SessionKey))
                    {
                        _logger.LogError(LoggingEvents.Session, $"セッションキー不一致 api.login_id={apiRegister.login_id} api.session_key={apiRegister.session_key} userStatus.SessionKey={userStatus?.SessionKey}");
                        await SendRegisterResultNGAsync(socket, nameof(ApiConstant.ERR04), ApiConstant.ERR04);
                        return;
                    }

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "セッションキーテーブルを更新する");
                    UpdateSessionInformation(socket, apiRegister);

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "作業状況配列をループ処理する");
                    foreach (var item in apiRegister.work_list)
                    {
                        bool lastItem = apiRegister.work_list.Last().Equals(item);

                        switch (item.work_type)
                        {
                            case ApiConstant.WORK_TYPE_WS:
                                await RegisterWsControllerAsync(socket, item, lastItem);
                                break;

                            case ApiConstant.WORK_TYPE_RF:
                                await RegisterRfControllerAsync(socket, item, lastItem);
                                break;

                            case ApiConstant.WORK_TYPE_SE:
                                await RegisterSeControllerAsync(socket, item, lastItem);
                                break;

                            default:
                                // バリデーションチェック済みなのでここは通らない
                                _logger.LogError(LoggingEvents.Validation, $"バリデーションエラー {item.work_type}");
                                break;
                        }
                    }
                }
                catch (JsonWriterException ex)
                {
                    _logger.LogError(LoggingEvents.ApiFormat, $"装置間IFフォーマットに変換できませんでした。{ex.Message}");
                    await SendRegisterResultNGAsync(socket, nameof(ApiConstant.ERR90), ApiConstant.ERR90);
                }
                catch (JsonReaderException ex)
                {
                    _logger.LogError(LoggingEvents.ApiFormat, $"装置間IFフォーマットではありません。{ex.Message}");
                    await SendRegisterResultNGAsync(socket, nameof(ApiConstant.ERR90), ApiConstant.ERR90);
                }
                catch (Exception ex)
                {
                    _logger.LogError(LoggingEvents.Exception, ex, "RegisterControllerAsync unknown error.");
                    await SendRegisterResultNGAsync(socket, nameof(ApiConstant.ERR90), ApiConstant.ERR90);
                }
            }
        }
        /// <summary>
        /// 作業状況登録コントローラ WS作業アイテム1個分
        /// </summary>
        /// <param name="socket">要求元</param>
        /// <param name="item">受信メッセージ</param>
        /// <param name="lastItem">最後のアイテムであれば真</param>
        /// <returns></returns>
        private async Task RegisterWsControllerAsync(WebSocket socket, ApiRegister.Work item, bool lastItem)
        {
            _logger.LogInformation(LoggingEvents.RegisterControllerAsync, "RegisterWsControllerAsync START");

            var options = new DbContextOptionsBuilder<GPnaviServerContext>()
                .UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
                .Options;
            using (var context = new GPnaviServerContext(options))
            {
                try
                {
                    var (error, registerDate) = GetDateTimeFromString(item.register_date);
                    // バリデーション済みなのでここでエラー判定は不要

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "WSマスタ検索");
                    var workScheduleMaster = context.WorkScheduleMasters.FirstOrDefault(e =>
                      e.Version == long.Parse(item.ws_version) &&
                      e.Start.Equals(item.ws_start) &&
                      e.Name.Equals(item.ws_name) &&
                      e.Holiday.Equals(item.ws_holiday));

                    if (workScheduleMaster == null)
                    {
                        _logger.LogError(LoggingEvents.Validation, $"作業がマスタに存在しない version={item.ws_version} start={item.ws_start} name={item.ws_name} holiday={item.ws_holiday}");
                        if (lastItem)
                        {
                            _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "最後のアイテムなので登録NGを送信する");
                            await SendRegisterResultNGAsync(socket, nameof(ApiConstant.ERR05), ApiConstant.ERR05);
                        }
                        else
                        {
                            _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "最後のアイテムではないので登録NGは送信しない");
                        }
                        return;
                    }

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "WS作業状態の古いデータをチェック");
                    var timeSpan = new TimeSpan(WS_STATUS_LIMIT, 0, 0);
                    var limitDateTime = DateTime.Now - timeSpan;
                    var removeItems = context.WorkScheduleStatuses.Where(e => e.StatusUpdateDate < limitDateTime);
                    if (removeItems.Count() > 0)
                    {
                        try
                        {
                            _logger.LogInformation(LoggingEvents.RegisterControllerAsync, "WS作業状態の有効期限切れデータを削除");
                            context.WorkScheduleStatuses.RemoveRange(removeItems);
                            // 保存を一か所にすることで例外回避するか様子を見る
                            //await _context.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(LoggingEvents.Exception, ex, "RegisterWsControllerAsync 有効期限切れ解放中にunknown error.");
                        }
                    }

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "WS作業状態検索");
                    var workScheduleStatus = context.WorkScheduleStatuses.FirstOrDefault(e =>
                      e.Version == workScheduleMaster.Version &&
                      e.Start.Equals(workScheduleMaster.Start) &&
                      e.Name.Equals(workScheduleMaster.Name) &&
                      e.Holiday.Equals(workScheduleMaster.Holiday));

                    if (workScheduleStatus != null)
                    {
                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "WS作業状態をチェック");
                        if (!workScheduleStatus.Status.Equals(ApiConstant.WORK_STATUS_CANCEL))
                        {
                            _logger.LogTrace(LoggingEvents.RegisterControllerAsync, $"キャンセル以外 {workScheduleStatus.Status} なので担当者IDをチェック");
                            if (!workScheduleStatus.LoginId.Equals(item.login_id))
                            {
                                _logger.LogError(LoggingEvents.Validation, $"他の担当者が作業中 version={item.ws_version} start={item.ws_start} name={item.ws_name} holiday={item.ws_holiday} login={item.login_id} status.login={workScheduleStatus.LoginId}");
                                if (lastItem)
                                {
                                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "最後のアイテムなので登録NGを送信する");
                                    await SendRegisterResultNGAsync(socket, nameof(ApiConstant.ERR06), ApiConstant.ERR06);
                                }
                                else
                                {
                                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "最後のアイテムではないので登録NGは送信しない");
                                }
                                return;
                            }
                        }

                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "WS作業状態を更新");
                        workScheduleStatus.Status = item.work_status;
                        if (item.work_status.Equals(ApiConstant.WORK_STATUS_START))
                        {
                            _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "作業開始日付時刻を更新");
                            workScheduleStatus.StartDate = registerDate;
                        }
                        workScheduleStatus.StatusUpdateDate = registerDate;
                        workScheduleStatus.LoginId = item.login_id;
                    }
                    else
                    {
                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "WS作業状態を追加");
                        workScheduleStatus = new WorkScheduleStatus
                        {
                            Version = workScheduleMaster.Version,
                            Start = workScheduleMaster.Start,
                            Name = workScheduleMaster.Name,
                            Holiday = workScheduleMaster.Holiday,
                            Status = item.work_status,
                            StartDate = registerDate,
                            StatusUpdateDate = registerDate,
                            LoginId = item.login_id
                        };
                        context.WorkScheduleStatuses.Add(workScheduleStatus);
                    }

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "担当者マスタを検索する（担当者名が必要）");
                    var userMaster = context.UserMasters.FirstOrDefault(e => e.LoginId.Equals(item.login_id));

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "作業状況履歴を追加");
                    var workStatusHistory = new WorkStatusHistory
                    {
                        Version = workScheduleStatus.Version,
                        Start = workScheduleStatus.Start,
                        Name = workScheduleStatus.Name,
                        Holiday = workScheduleStatus.Holiday,
                        RfType = "",
                        SensorId = "",
                        DisplayName = workScheduleMaster.ShortName,
                        LoginId = item.login_id,
                        LoginName = userMaster?.LoginName,
                        Status = workScheduleStatus.Status,
                        StartDate = workScheduleStatus.StartDate,
                        RegisterDate = registerDate
                    };
                    if (!IsExistPrimaryKeyInWorkStatusHistory(context, workStatusHistory))
                    {
                        context.WorkStatusHistories.Add(workStatusHistory);
                    }

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "保存する");
                    await context.SaveChangesAsync();

                    if (lastItem)
                    {
                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "最後のアイテムなので登録OKを送信する");
                        var apiRegisterResult = new ApiRegisterResult
                        {
                            message_name = ApiConstant.MESSAGE_REGISTER_RESULT,
                            result = ApiConstant.RESULT_OK
                        };
                        var resultJson = JsonConvert.SerializeObject(apiRegisterResult);
                        await SendMessageAsync(socket, resultJson);
                    }
                    else
                    {
                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "最後のアイテムではないので登録OKは送信しない");
                    }

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "作業ステータスを送信する");
                    var apiWorkStatus = new ApiWorkStatus
                    {
                        message_name = ApiConstant.MESSAGE_WORK_STATUS,
                        work_status = workScheduleStatus.Status,
                        ws_version = workScheduleStatus.Version.ToString(),
                        ws_start = workScheduleStatus.Start,
                        ws_name = workScheduleStatus.Name,
                        ws_holiday = workScheduleStatus.Holiday,
                        start_date = workScheduleStatus.StartDate.ToString(DateTimeFormat, CultureInfoApi),
                        register_date = workScheduleStatus.StatusUpdateDate.ToString(DateTimeFormat, CultureInfoApi)
                    };
                    var workStatusJson = JsonConvert.SerializeObject(apiWorkStatus);
                    await SendWorkStatusMessageToAllAsync(context, workStatusJson);
                }
                catch (Exception ex)
                {
                    _logger.LogError(LoggingEvents.Exception, ex, "RegisterControllerAsync unknown error.");
                    if (lastItem)
                    {
                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "最後のアイテムなので登録NGを送信する");
                        await SendRegisterResultNGAsync(socket, nameof(ApiConstant.ERR90), ApiConstant.ERR90);
                    }
                    else
                    {
                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "最後のアイテムではないので登録NGは送信しない");
                    }
                    return;
                }
            }
        }
        /// <summary>
        /// 作業状況登録コントローラ レジFF作業アイテム1個分
        /// </summary>
        /// <param name="socket">要求元</param>
        /// <param name="item">受信メッセージ</param>
        /// <param name="lastItem">最後のアイテムであれば真</param>
        /// <returns></returns>
        private async Task RegisterRfControllerAsync(WebSocket socket, ApiRegister.Work item, bool lastItem)
        {
            _logger.LogInformation(LoggingEvents.RegisterControllerAsync, "RegisterRfControllerAsync START");

            var options = new DbContextOptionsBuilder<GPnaviServerContext>()
                .UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
                .Options;
            using (var context = new GPnaviServerContext(options))
            {
                try
                {
                    var (error, registerDate) = GetDateTimeFromString(item.register_date);
                    // バリデーション済みなのでここでエラー判定は不要

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "担当者マスタを検索する（担当者名が必要）");
                    var userMaster = context.UserMasters.FirstOrDefault(e => e.LoginId.Equals(item.login_id));

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "作業状況履歴を追加");
                    var displayName = item.rf_type.Equals(ApiConstant.RF_TYPE_REG) ? DISPLAY_NAME_REG : DISPLAY_NAME_FF;
                    var workStatusHistory = new WorkStatusHistory
                    {
                        Version = 0,
                        Start = "",
                        Name = "",
                        Holiday = "",
                        // #112 レジFF種別の登録を追加
                        RfType = item.rf_type,
                        SensorId = "",
                        DisplayName = displayName,
                        LoginId = item.login_id,
                        LoginName = userMaster?.LoginName,
                        Status = item.work_status,
                        StartDate = registerDate,
                        RegisterDate = registerDate
                    };
                    if (!IsExistPrimaryKeyInWorkStatusHistory(context, workStatusHistory))
                    {
                        context.WorkStatusHistories.Add(workStatusHistory);
                    }

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "保存する");
                    await context.SaveChangesAsync();

                    if (lastItem)
                    {
                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "最後のアイテムなので登録OKを送信する");
                        var apiRegisterResult = new ApiRegisterResult
                        {
                            message_name = ApiConstant.MESSAGE_REGISTER_RESULT,
                            result = ApiConstant.RESULT_OK
                        };
                        var resultJson = JsonConvert.SerializeObject(apiRegisterResult);
                        await SendMessageAsync(socket, resultJson);
                    }
                    else
                    {
                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "最後のアイテムではないので登録OKは送信しない");
                    }

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "応援フラグをチェックする");
                    if (item.rf_help.Equals(ApiConstant.HELP_ON))
                    {
                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "応援フラグONの場合は作業ステータスを送信する");
                        var apiWorkStatus = new ApiWorkStatus
                        {
                            message_name = ApiConstant.MESSAGE_WORK_STATUS,
                            work_status = item.work_status,
                            // #117 レジFF種別を追加
                            rf_type = item.rf_type,
                            register_date = registerDate.ToString(DateTimeFormat, CultureInfoApi)
                        };
                        var workStatusJson = JsonConvert.SerializeObject(apiWorkStatus);
                        await SendWorkStatusMessageToAllAsync(context, workStatusJson);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(LoggingEvents.Exception, ex, "RegisterControllerAsync unknown error.");
                    if (lastItem)
                    {
                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "最後のアイテムなので登録NGを送信する");
                        await SendRegisterResultNGAsync(socket, nameof(ApiConstant.ERR90), ApiConstant.ERR90);
                    }
                    else
                    {
                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "最後のアイテムではないので登録NGは送信しない");
                    }
                    return;
                }
            }
        }
        /// <summary>
        /// 作業状況登録コントローラ 突発作業アイテム1個分
        /// </summary>
        /// <param name="socket">要求元</param>
        /// <param name="item">受信メッセージ</param>
        /// <param name="lastItem">最後のアイテムであれば真</param>
        /// <returns></returns>
        private async Task RegisterSeControllerAsync(WebSocket socket, ApiRegister.Work item, bool lastItem)
        {
            _logger.LogInformation(LoggingEvents.RegisterControllerAsync, "RegisterSeControllerAsync START");

            var options = new DbContextOptionsBuilder<GPnaviServerContext>()
                .UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
                .Options;
            using (var context = new GPnaviServerContext(options))
            {
                try
                {
                    var (error, registerDate) = GetDateTimeFromString(item.register_date);
                    // バリデーション済みなのでここでエラー判定は不要

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "突発作業状態を検索する");
                    var sensorStatus = context.SensorStatuses.FirstOrDefault(e => e.SensorId.Equals(item.sensor_id));
                    if (sensorStatus != null)
                    {
                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "存在するので作業状態をチェック");
                        if (!string.IsNullOrWhiteSpace(sensorStatus.Status) &&
                            !(sensorStatus.Status.Equals(ApiConstant.WORK_STATUS_CANCEL) ||
                            sensorStatus.Status.Equals(ApiConstant.WORK_STATUS_FINISH)))
                        {
                            _logger.LogTrace(LoggingEvents.RegisterControllerAsync, $"キャンセル、完了以外 {sensorStatus.Status} なので担当者IDをチェック");
                            if (!string.IsNullOrWhiteSpace(sensorStatus.LoginId) &&
                                !sensorStatus.LoginId.Equals(item.login_id))
                            {
                                _logger.LogTrace(LoggingEvents.RegisterControllerAsync, $"他の担当者が作業中 login={item.login_id} status.login={sensorStatus.LoginId}");
                                if (lastItem)
                                {
                                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "最後のアイテムなので登録NGを送信する");
                                    await SendRegisterResultNGAsync(socket, nameof(ApiConstant.ERR06), ApiConstant.ERR06);
                                }
                                else
                                {
                                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "最後のアイテムではないので登録NGは送信しない");
                                }
                                return;
                            }
                        }

                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "突発作業状態を更新");
                        if (item.work_status.Equals(ApiConstant.WORK_STATUS_START))
                        {
                            _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "作業開始日付時刻を更新");
                            sensorStatus.StartDate = registerDate;
                        }
                        sensorStatus.Status = item.work_status;
                        sensorStatus.LoginId = item.login_id;
                    }
                    else
                    {
                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "センサマスタを検索する（表示用作業名等が必要）");
                        var sensorMaster = context.SensorMasters.FirstOrDefault(e => e.SensorId.Equals(item.sensor_id));

                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "存在しないので突発作業状態を追加する");
                        sensorStatus = new SensorStatus
                        {
                            SensorId = item.sensor_id,
                            SensorType = sensorMaster?.SensorType ?? "",
                            DisplayName = sensorMaster?.DisplayName ?? "",
                            OccurrenceDate = registerDate,
                            StartDate = registerDate,
                            Status = item.work_status,
                            LoginId = item.login_id
                        };
                        context.SensorStatuses.Add(sensorStatus);
                    }

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "担当者マスタを検索する（担当者名が必要）");
                    var userMaster = context.UserMasters.FirstOrDefault(e => e.LoginId.Equals(item.login_id));

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "作業状況履歴を追加");
                    var workStatusHistory = new WorkStatusHistory
                    {
                        Version = 0,
                        Start = "",
                        Name = "",
                        Holiday = "",
                        RfType = "",
                        SensorId = sensorStatus.SensorId,
                        DisplayName = sensorStatus.DisplayName,
                        LoginId = item.login_id,
                        LoginName = userMaster?.LoginName,
                        Status = sensorStatus.Status,
                        StartDate = sensorStatus.StartDate,
                        RegisterDate = registerDate
                    };
                    if (!IsExistPrimaryKeyInWorkStatusHistory(context, workStatusHistory))
                    {
                        context.WorkStatusHistories.Add(workStatusHistory);
                    }

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "保存する");
                    await context.SaveChangesAsync();

                    if (lastItem)
                    {
                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "最後のアイテムなので登録OKを送信する");
                        var apiRegisterResult = new ApiRegisterResult
                        {
                            message_name = ApiConstant.MESSAGE_REGISTER_RESULT,
                            result = ApiConstant.RESULT_OK
                        };
                        var resultJson = JsonConvert.SerializeObject(apiRegisterResult);
                        await SendMessageAsync(socket, resultJson);
                    }
                    else
                    {
                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "最後のアイテムではないので登録OKは送信しない");
                    }

                    _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "作業ステータスを送信する");
                    var apiWorkStatus = new ApiWorkStatus
                    {
                        message_name = ApiConstant.MESSAGE_WORK_STATUS,
                        work_status = sensorStatus.Status,
                        sensor_id = sensorStatus.SensorId,
                        start_date = sensorStatus.StartDate.ToString(DateTimeFormat, CultureInfoApi)
                    };
                    var workStatusJson = JsonConvert.SerializeObject(apiWorkStatus);
                    await SendWorkStatusMessageToAllAsync(context, workStatusJson);
                }
                catch (Exception ex)
                {
                    _logger.LogError(LoggingEvents.Exception, ex, "RegisterControllerAsync unknown error.");
                    if (lastItem)
                    {
                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "最後のアイテムなので登録NGを送信する");
                        await SendRegisterResultNGAsync(socket, nameof(ApiConstant.ERR90), ApiConstant.ERR90);
                    }
                    else
                    {
                        _logger.LogTrace(LoggingEvents.RegisterControllerAsync, "最後のアイテムではないので登録NGは送信しない");
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// 受信メッセージバリデーション 作業状況登録
        /// </summary>
        /// <param name="apiRegister">受信メッセージ</param>
        /// <returns>バリデーションエラーの場合は真</returns>
        private (bool result, string message) IsInvalidApiRegister(ApiRegister apiRegister)
        {
            if (apiRegister == null)
            {
                return (true, "apiRegister is null.");
            }

            var (result, message) = IsInvalidApiCommonUp(apiRegister);
            if (result)
            {
                return (result, message);
            }

            if (apiRegister.work_list == null)
            {
                return (true, "work_list is null.");
            }

            if (apiRegister.work_list.Count == 0)
            {
                return (true, "work_list.Count is zero.");
            }

            foreach (var item in apiRegister.work_list)
            {
                if (string.IsNullOrWhiteSpace(item.login_id))
                {
                    return (true, "login_id is null.");
                }

                if (string.IsNullOrWhiteSpace(item.register_date))
                {
                    return (true, "register_date is null.");
                }

                var (error, dateTime) = GetDateTimeFromString(item.register_date);
                if (error)
                {
                    return (true, $"register_date {item.register_date} is invalid.");
                }

                if (string.IsNullOrWhiteSpace(item.work_status))
                {
                    return (true, "work_status is null.");
                }

                switch (item.work_status)
                {
                    case ApiConstant.WORK_STATUS_START:
                    case ApiConstant.WORK_STATUS_PAUSE:
                    case ApiConstant.WORK_STATUS_CANCEL:
                    case ApiConstant.WORK_STATUS_FINISH:
                        break;

                    default:
                        return (true, $"work_status {item.work_status} is invalid.");
                }

                if (string.IsNullOrWhiteSpace(item.work_type))
                {
                    return (true, "work_type is null.");
                }

                if (item.work_type == ApiConstant.WORK_TYPE_WS)
                {
                    // WS作業
                    if (string.IsNullOrWhiteSpace(item.ws_version))
                    {
                        return (true, "ws_version is null.");
                    }

                    if (string.IsNullOrWhiteSpace(item.ws_start))
                    {
                        return (true, "ws_start is null.");
                    }

                    if (string.IsNullOrWhiteSpace(item.ws_name))
                    {
                        return (true, "ws_name is null.");
                    }

                    if (string.IsNullOrWhiteSpace(item.ws_holiday))
                    {
                        return (true, "ws_holiday is null.");
                    }

                    switch (item.ws_holiday)
                    {
                        case ApiConstant.HOLIDAY_FALSE:
                        case ApiConstant.HOLIDAY_TRUE:
                            break;

                        default:
                            return (true, $"ws_holiday {item.ws_holiday} is invalid.");
                    }
                }
                else if (item.work_type == ApiConstant.WORK_TYPE_RF)
                {
                    // レジFF作業
                    if (string.IsNullOrWhiteSpace(item.rf_type))
                    {
                        return (true, "rf_type is null.");
                    }

                    switch (item.rf_type)
                    {
                        case ApiConstant.RF_TYPE_REG:
                        case ApiConstant.RF_TYPE_FF:
                            break;

                        default:
                            return (true, $"rf_type {item.rf_type} is invalid.");
                    }

                    if (string.IsNullOrWhiteSpace(item.rf_help))
                    {
                        return (true, "rf_help is null.");
                    }

                    switch (item.rf_help)
                    {
                        case ApiConstant.HELP_OFF:
                        case ApiConstant.HELP_ON:
                            break;

                        default:
                            return (true, $"rf_help {item.rf_help} is invalid.");
                    }
                }
                else if (item.work_type == ApiConstant.WORK_TYPE_SE)
                {
                    // センサー
                    if (string.IsNullOrWhiteSpace(item.sensor_id))
                    {
                        return (true, "sensor_id is null.");
                    }
                }
                else
                {
                    // 該当なし
                    return (true, $"work_type {item.work_type} is invalid.");
                }
            }

            return (false, null);
        }
        /// <summary>
        /// 装置間IFの日付時刻をDateTime型に変換する
        /// </summary>
        /// <param name="dateString">日付時刻文字列</param>
        /// <returns>パースエラーの場合は真</returns>
        private (bool error, DateTime dateTime) GetDateTimeFromString(string dateString)
        {
            try
            {
                var dateTime = DateTime.ParseExact(dateString, DateTimeFormat, CultureInfoApi);
                return (false, dateTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(LoggingEvents.Exception, ex, $"GetDateTimeFromString dateString={dateString}");
                return (true, default(DateTime));
            }
        }

        /// <summary>
        /// 登録結果NGを送信する
        /// </summary>
        /// <param name="socket">要求元</param>
        /// <param name="code">エラーコード</param>
        /// <param name="message">エラーメッセージ</param>
        /// <returns></returns>
        private async Task SendRegisterResultNGAsync(WebSocket socket, string code, string message)
        {
            try
            {
                var apiRegisterResult = new ApiRegisterResult
                {
                    message_name = ApiConstant.MESSAGE_REGISTER_RESULT,
                    result = ApiConstant.RESULT_NG,
                    error_code = code,
                    error_message = message
                };
                var json = JsonConvert.SerializeObject(apiRegisterResult);
                await SendMessageAsync(socket, json);
            }
            catch (Exception ex)
            {
                // 送信後にNGを返すシーケンスはないのでここで終端する
                _logger.LogError(LoggingEvents.Exception, ex, $"登録結果NGの送信に失敗しました。{code} {message}");
            }
        }
        /// <summary>
        /// レジFF応援コントローラ
        /// </summary>
        /// <param name="socket">要求元</param>
        /// <param name="json">受信メッセージ</param>
        /// <returns></returns>
        private async Task HelpRequestControllerAsync(WebSocket socket, string json)
        {
            _logger.LogInformation(LoggingEvents.HelpRequestControllerAsync, "HelpRequestControllerAsync START");

            var options = new DbContextOptionsBuilder<GPnaviServerContext>()
                .UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
                .Options;
            using (var context = new GPnaviServerContext(options))
            {
                try
                {
                    _logger.LogTrace(LoggingEvents.HelpRequestControllerAsync, "パース");
                    var apiHelpRequest = JsonConvert.DeserializeObject<ApiHelpRequest>(json);

                    _logger.LogTrace(LoggingEvents.HelpRequestControllerAsync, "バリデーションチェック");
                    var (result, message) = IsInvalidApiHelpRequest(apiHelpRequest);
                    if (result)
                    {
                        _logger.LogError(LoggingEvents.Validation, $"バリデーションエラー {message}");
                        return;
                    }

                    _logger.LogTrace(LoggingEvents.HelpRequestControllerAsync, "担当者ステータス検索");
                    var userStatus = context.UserStatuses.FirstOrDefault(e => e.LoginId.Equals(apiHelpRequest.login_id));

                    _logger.LogTrace(LoggingEvents.HelpRequestControllerAsync, "セッションキーチェック");
                    if (userStatus == null || string.IsNullOrEmpty(apiHelpRequest.session_key))
                    {
                        _logger.LogError(LoggingEvents.Session, $"セッションキーが無い api.login_id={apiHelpRequest.login_id} api.session_key={apiHelpRequest.session_key} userStatus.SessionKey={userStatus?.SessionKey}");
                        return;
                    }
                    if (!apiHelpRequest.session_key.Equals(userStatus.SessionKey))
                    {
                        _logger.LogError(LoggingEvents.Session, $"セッションキー不一致 api.login_id={apiHelpRequest.login_id} api.session_key={apiHelpRequest.session_key} userStatus.SessionKey={userStatus?.SessionKey}");
                        return;
                    }

                    _logger.LogTrace(LoggingEvents.HelpRequestControllerAsync, "セッションキーテーブルを更新する");
                    UpdateSessionInformation(socket, apiHelpRequest);

                    _logger.LogTrace(LoggingEvents.HelpRequestControllerAsync, "レジFF応援を送信する");
                    var apiHelpPush = new ApiHelpPush
                    {
                        message_name = ApiConstant.MESSAGE_HELP_PUSH,
                        rf_type = apiHelpRequest.rf_type,
                        login_id = apiHelpRequest.login_id
                    };
                    var helpPushJson = JsonConvert.SerializeObject(apiHelpPush);
                    //await PushMessageToAllAsync(helpPushJson);
                    // 確認用に全員宛に送信する場合は上を有効に
                    await PushMessageToAllWithoutIdAsync(context, helpPushJson, apiHelpRequest.login_id);
                    _logger.LogInformation(LoggingEvents.PushMessageAsync, $"レジFF応援の送信に成功しました。{helpPushJson}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(LoggingEvents.Exception, ex, "HelpRequestControllerAsync unknown error.");
                }
            }
        }
        /// <summary>
        /// 受信メッセージバリデーション レジFF応援
        /// </summary>
        /// <param name="apiHelpRequest">受信メッセージ</param>
        /// <returns>バリデーションエラーの場合は真</returns>
        private (bool result, string message) IsInvalidApiHelpRequest(ApiHelpRequest apiHelpRequest)
        {
            if (apiHelpRequest == null)
            {
                return (true, "apiHelpRequest is null.");
            }

            var (result, message) = IsInvalidApiCommonUp(apiHelpRequest);
            if (result)
            {
                return (result, message);
            }

            if (string.IsNullOrWhiteSpace(apiHelpRequest.rf_type))
            {
                return (true, "rf_type is null.");
            }

            if (!(apiHelpRequest.rf_type.Equals(ApiConstant.RF_TYPE_REG) || apiHelpRequest.rf_type.Equals(ApiConstant.RF_TYPE_FF)))
            {
                return (true, $"rf_type {apiHelpRequest.rf_type} is invalid.");
            }

            return (false, null);
        }
        /// <summary>
        /// Push送信 全員
        /// </summary>
        /// <param name="context">DBコンテキスト</param>
        /// <param name="message">送信メッセージ</param>
        /// <returns></returns>
        private async Task PushMessageToAllAsync(GPnaviServerContext context, string message)
        {
            try
            {
                var androidTokens = context?.UserStatuses.Where(e => ApiConstant.DEVICE_TYPE_ANDROID.Equals(e.DeviceType) && !string.IsNullOrWhiteSpace(e.DeviceToken))?.Select(e => e.DeviceToken)?.ToList();
                if (androidTokens != null && androidTokens.Count > 0)
                {
                    await PushToNotificationHub.SendNotificationGcmAsync(message, androidTokens);
                }

                var iotTokens = context?.UserStatuses.Where(e => ApiConstant.DEVICE_TYPE_IOT.Equals(e.DeviceType) && !string.IsNullOrWhiteSpace(e.DeviceToken))?.Select(e => e.DeviceToken)?.ToList();
                if (iotTokens != null && iotTokens.Count > 0)
                {
                    await PushToNotificationHub.SendNotificationWindowsAsync(message, iotTokens);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        /// <summary>
        /// Push送信 要求元以外
        /// </summary>
        /// <param name="context">DBコンテキスト</param>
        /// <param name="message">送信メッセージ</param>
        /// <param name="loginId">要求元の担当者ID</param>
        /// <returns></returns>
        private async Task PushMessageToAllWithoutIdAsync(GPnaviServerContext context, string message, string loginId)
        {
            try
            {
                var androidTokens = context?.UserStatuses.Where(e => ApiConstant.DEVICE_TYPE_ANDROID.Equals(e.DeviceType) && !string.IsNullOrWhiteSpace(e.DeviceToken) && !e.LoginId.Equals(loginId))?.Select(e => e.DeviceToken)?.ToList();
                if (androidTokens != null && androidTokens.Count > 0)
                {
                    await PushToNotificationHub.SendNotificationGcmAsync(message, androidTokens);
                }

                var iotTokens = context?.UserStatuses.Where(e => ApiConstant.DEVICE_TYPE_IOT.Equals(e.DeviceType) && !string.IsNullOrWhiteSpace(e.DeviceToken) && !e.LoginId.Equals(loginId))?.Select(e => e.DeviceToken)?.ToList();
                if (iotTokens != null && iotTokens.Count > 0)
                {
                    await PushToNotificationHub.SendNotificationWindowsAsync(message, iotTokens);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task ListRequestController(WebSocket socket, string json)
        {
            _logger.LogInformation(LoggingEvents.ListRequestControllerAsync, "ListRequestController START");
            var options = new DbContextOptionsBuilder<GPnaviServerContext>()
                .UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
                .Options;
            using (var context = new GPnaviServerContext(options))
            {

                try
                {
                    Stopwatch sw = new Stopwatch();
                    var resource = JObject.Parse(json);

                    //担当者Id
                    var loginId = resource.Properties().First(prop => "login_id" == prop.Name).Value.ToString();

                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"担当者マスタの検索");

                    var sessionKey = resource.Properties().First(prop => "session_key" == prop.Name).Value.ToString();

                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"担当者ステータスの検索");
                    var userStatus = context.UserStatuses.Find(loginId);

                    //セッションキーが存在しない[ERR03]
                    if (null == userStatus || string.IsNullOrEmpty(userStatus.SessionKey))
                    {
                        _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"セッションキーが無い");
                        await SendListResultNGAsync(socket, nameof(ApiConstant.ERR03), ApiConstant.ERR03);
                        return;
                    }

                    //セッションキーが不一致[ERR04]
                    if (!string.Equals(sessionKey, userStatus.SessionKey))
                    {
                        _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"セッションキー不一致");
                        await SendListResultNGAsync(socket, nameof(ApiConstant.ERR04), ApiConstant.ERR04);
                        return;
                    }

                    //①WS作業開始時刻には日付が存在しないため、現在日時の-12時間～+12時間の範囲のWS作業開始日時を生成する。
                    var now = DateTime.Now;
                    var startTime = now.AddHours(-12);
                    var endTime = now.AddHours(12);

                    //WSマスタバージョン取得。もし存在しないため、WSマスタが存在しない[ERR07]
                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"WSマスタバージョンの検索");
                    sw.Start();
                    var wsvs = context.WorkScheduleVersions.AsNoTracking().Where(wsver => wsver.ExpirationDate > startTime);
                    sw.Stop();
                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"WSマスタバージョンの検索経過時間の合計 = {sw.Elapsed}");

                    if (wsvs == null || wsvs.Count() < 1)
                    {
                        _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"WSマスタバージョンが無い");
                        await SendListResultNGAsync(socket, nameof(ApiConstant.ERR07), ApiConstant.ERR07);
                        return;
                    }

                    //有効的なVersionのArrayを取得
                    var verArr = wsvs.AsNoTracking().Select(enty => enty.Id).ToArray();

                    //WSマスタから、上記で生成したWS作業開始日時が、各WSマスタバージョンの有効期限内の作業を取得する。
                    //WSマスタ取得。もしが存在しない[ERR07]
                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"WSマスタの検索");
                    sw.Restart();  
                    var wsms = context.WorkScheduleMasters.AsNoTracking().ToList().Where(e => verArr.Contains(e.Version) );
                    wsms = wsms.Where(item => {

                        //休日判別で不具合なデータをフィルターする（正しいデータのみを選択）
                        var dayMiddle = DateTime.Today.AddHours(12);
                        var startDateTimeTmp = DateTime.Today.Add(TimeSpan.Parse(item.Start));

                        if (now <= dayMiddle && startDateTimeTmp > endTime)
                        {
                            startDateTimeTmp = startDateTimeTmp.AddHours(-24);
                        }
                        else if(now > dayMiddle && startDateTimeTmp < startTime )
                        {
                            startDateTimeTmp = startDateTimeTmp.AddHours(24);
                        }

                        return string.Equals(item.Holiday, GetHolidayFlag(context, startDateTimeTmp));
                    });

                    sw.Stop();
                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"WSマスタの検索経過時間の合計 = {sw.Elapsed}");

                    if (wsms == null || wsms.Count() < 1)
                    {
                        _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"WSマスタが無い");
                        await SendListResultNGAsync(socket, nameof(ApiConstant.ERR07), ApiConstant.ERR07);
                        return;
                    }

                    //②作業状況履歴テーブルから、作業履歴の生成																													
                    //作業状況履歴テーブルから、現在日時から - 12時間の作業履歴を取得する。																													
                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"作業状況履歴の検索");
                    // TODO 例外しないための応急処置
                    var timeSpan = default(TimeSpan);
                    sw.Restart();
                    var wshs = context.WorkStatusHistories.AsNoTracking().ToList().Where(e => DateTime.Today.Add((TimeSpan.TryParse(e.Start, out timeSpan)) ? timeSpan : default(TimeSpan)) >= startTime);
                    sw.Stop();
                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"作業状況履歴の検索経過時間の合計 = {sw.Elapsed}");

                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"突発作業状態の検索");
                    sw.Restart();
                    var sss = context.SensorStatuses.AsNoTracking().ToList();
                    sw.Stop();
                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"突発作業状態の検索経過時間の合計 = {sw.Elapsed}");

                    if (wshs.Count() > 0)
                    {
                        _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"検索条件の設定");
                        //同じ作業のデータが複数存在する場合は、最後のデータを抽出対象とする。

                        //ただし、最後のデータの作業状況履歴がキャンセルのデータは、抽出対象外とする。    // TODO: 確認必要  
                        const string WORK_STATUS_CANCEL = ApiConstant.WORK_STATUS_CANCEL;// TODO: 確認必要 const string WORK_STATUS_CANCEL = "キャンセル";

                        sw.Restart();
                        //WSバージョン番号、WS作業開始時間、WS作業名、WS休日区分、作業開始日付時刻　で重複判別
                        wshs = wshs.GroupBy(item => new { item.Version, item.Start, item.Name, item.Holiday, item.StartDate })
                            .SelectMany(grouping => grouping.Where(item => item.Status != WORK_STATUS_CANCEL).OrderByDescending(item => item.RegisterDate).Take(1));

                        //レジFF種別、担当者ID、作業開始日付時刻　で重複判別
                        wshs = wshs.GroupBy(item => new { item.RfType, item.LoginId, item.StartDate })
                            .SelectMany(grouping => grouping.Where(item => item.Status != WORK_STATUS_CANCEL).OrderByDescending(item => item.RegisterDate).Take(1));

                        //センサーID、作業開始日付時刻　で重複判別
                        wshs = wshs.GroupBy(item => new { item.SensorId, item.StartDate })
                            .SelectMany(grouping => grouping.Where(item => item.Status != WORK_STATUS_CANCEL).OrderByDescending(item => item.RegisterDate).Take(1));

                        //休日判別。入力引数：「登録日付時刻」
                        //wshs = wshs.ToList().Where(item => string.Equals(item.Holiday, GetHolidayFlag(context, item.RegisterDate)));

                        sw.Stop();
                        _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"wshs検索条件の設定経過時間の合計 = {sw.Elapsed}");

                    }
                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"検索条件でSELECT");
                    var wshsExt = from wsh in wshs
                                  join ss in sss
                                  on wsh.SensorId equals ss.SensorId into wshs_sss
                                  from rec in wshs_sss.DefaultIfEmpty()
                                  select new
                                  {
                                      wsh.Version,
                                      wsh.Start,
                                      wsh.Name,
                                      wsh.Holiday,
                                      wsh.RfType,
                                      wsh.SensorId,
                                      wsh.DisplayName,
                                      wsh.LoginId,
                                      wsh.LoginName,
                                      wsh.Status,
                                      wsh.StartDate,
                                      wsh.RegisterDate,
                                      SensorType = rec?.SensorType ?? "",
                                      OccurrenceDate = rec?.OccurrenceDate ?? default(DateTime),
                                  };
                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"検索条件でSELECT結果をリストに");

                    sw.Restart();
                    wshsExt = wshsExt.ToList();
                    sw.Stop();
                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"wshsExt.ToList() 経過時間の合計 = {sw.Elapsed}");


                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"検索条件でさらにSELECT");
                    var wsms_leftOuterJoin_wshsExt = from wsm in wsms
                                                     join wshExt in wshsExt
                                                     on new { wsm.Version, wsm.Start, wsm.Name, wsm.Holiday } equals new { wshExt.Version, wshExt.Start, wshExt.Name, wshExt.Holiday }
                                                     into temp
                                                     from rec in temp.DefaultIfEmpty()
                                                     select new
                                                     {
                                                         display_date = wsm.Start,
                                                         ws_version = wsm.Version,
                                                         ws_start = wsm.Start,
                                                         ws_name = wsm.Name,
                                                         ws_holiday = wsm.Holiday,
                                                         ws_priority = wsm.Priority,
                                                         sensor_id = rec?.SensorId ?? "",
                                                         sensor_type = rec?.SensorType ?? "",
                                                         rf_type = rec?.RfType ?? "",
                                                         ws_icon_id = wsm.IconId,
                                                         display_short_name = wsm.ShortName,
                                                         login_id = rec?.LoginId ?? "",
                                                         login_name = rec?.LoginName ?? "",
                                                         work_status = rec?.Status ?? "",
                                                         sensor_date = rec?.OccurrenceDate ?? default(DateTime),
                                                         start_date = rec?.StartDate ?? default(DateTime),
                                                         register_date = rec?.RegisterDate ?? default(DateTime),
                                                     };


                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"検索条件でさらにさらにSELECT");
                    var wsms_rightOuterJoin_wshsExt = from wshExt in wshsExt
                                                      join wsm in wsms
                                                      on new { wshExt.Version, wshExt.Start, wshExt.Name, wshExt.Holiday } equals new { wsm.Version, wsm.Start, wsm.Name, wsm.Holiday }
                                                      into temp
                                                      from rec in temp.DefaultIfEmpty()
                                                      select new
                                                      {
                                                          display_date = wshExt.Start,
                                                          ws_version = wshExt.Version,
                                                          ws_start = rec?.Start ?? "",
                                                          ws_name = wshExt.Name,
                                                          ws_holiday = wshExt.Holiday,
                                                          ws_priority = rec?.Priority ?? "",
                                                          sensor_id = wshExt.SensorId,
                                                          sensor_type = wshExt.SensorType,
                                                          rf_type = wshExt.RfType,
                                                          ws_icon_id = rec?.IconId ?? "",
                                                          display_short_name = wshExt.DisplayName,
                                                          login_id = wshExt.LoginId,
                                                          login_name = wshExt.LoginName,
                                                          work_status = wshExt.Status,
                                                          sensor_date = wshExt.OccurrenceDate == null ? default(DateTime) : wshExt.OccurrenceDate,
                                                          start_date = wshExt.StartDate == null ? default(DateTime) : wshExt.StartDate,
                                                          register_date = wshExt.RegisterDate == null ? default(DateTime) : wshExt.RegisterDate,
                                                      };

                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"検索条件でさらにさらにさらにUNION");

                    sw.Restart();
                    var wsms_fullOuterJoin_wshsExt = wsms_leftOuterJoin_wshsExt.Union(wsms_rightOuterJoin_wshsExt);
                    sw.Stop();
                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"wsms_fullOuterJoin_wshsEx 経過時間の合計 = {sw.Elapsed}");


                    //③作業状況照会データの生成
                    //    ①と②を結合し、作業状況照会データを生成する。	※①wsmsと②wshs_extを結合は[FULL JOIN]					 																																							
                    //    ②作業履歴が存在する場合は、作業状況履歴のデータを出力し、存在しない場合は、①WS作業一覧のデータを出力する。

                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"長い検索の後でリストに");

                    sw.Restart();
                    var listResult = wsms_fullOuterJoin_wshsExt.ToList();
                    sw.Stop();
                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"長い検索の後でリストに 経過時間の合計 = {sw.Elapsed}");

                    var wsList = new List<WorkItem>();

                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"レスポンスを作り始める count={listResult.Count}");

                    sw.Restart();
                    listResult.ForEach(item =>
                    {
                        _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"レスポンスを作り中...");

                        var display_date_tmp = DateTime.Today.Add(TimeSpan.Parse(item.display_date));
                        if (display_date_tmp > endTime)
                        {
                            display_date_tmp = display_date_tmp.AddHours(-24);
                        }
                        else if (display_date_tmp < startTime)
                        {
                            display_date_tmp = display_date_tmp.AddHours(24);
                        }
                        wsList.Add(new WorkItem
                        {
                            display_date = display_date_tmp.ToString(DateTimeFormat, CultureInfoApi),
                            ws_version = item.ws_version.ToString(),
                            ws_start = item.ws_start,
                            ws_name = item.ws_name,
                            ws_holiday = item.ws_holiday,
                            ws_priority = item.ws_priority,
                            sensor_id = item.sensor_id,
                            rf_type = item.rf_type,
                            ws_icon_id = item.ws_icon_id,
                            display_short_name = item.display_short_name,
                            login_id = item.login_id,
                            login_name = item.login_name,
                            work_status = item.work_status,
                            sensor_date = item.sensor_date == default(DateTime) ? "" : item.sensor_date.ToString(DateTimeFormat, CultureInfoApi),
                            start_date = item.start_date == default(DateTime) ? "" : item.start_date.ToString(DateTimeFormat, CultureInfoApi),
                            register_date = item.register_date == default(DateTime) ? "" : item.register_date.ToString(DateTimeFormat, CultureInfoApi)
                        });
                    });

                    wsList = wsList.OrderByDescending(item => item.display_date).ToList();
                    sw.Stop();
                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"レスポンスを作り 経過時間の合計 = {sw.Elapsed}");

                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, $"レスポンスを送信する" + DateTime.Now);
                    try
                    {
                        var apiListResult = new ApiListResult
                        {
                            message_name = ApiConstant.MESSAGE_LIST_RESULT,
                            error_code = "",
                            error_message = "",
                            result = ApiConstant.RESULT_OK,
                            work_list = wsList,
                        };
                        var listResultJson = JsonConvert.SerializeObject(apiListResult);
                        await SendMessageAsync(socket, listResultJson);
                        _logger.LogInformation(LoggingEvents.ListRequestControllerAsync, $"レスポンスを送信完了" + DateTime.Now);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(LoggingEvents.Exception, ex, $"作業状況一覧の送信に失敗しました。");
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(LoggingEvents.Exception, ex, "ListRequestController unknown error.");
                }

            }
        }

        /// <summary>
        /// 休日判定
        /// </summary>
        /// <param name="context">DBコンテキスト</param>
        /// <param name="datetime">「作業開始時間」或いは「登録日付時刻」</param>
        /// <returns>休日区分</returns>
        private string GetHolidayFlag(GPnaviServerContext context, DateTime datetime)
        {
            var date = default(DateTime);
            try
            {
                if (datetime == null)
                {
                    _logger.LogTrace(LoggingEvents.ListRequestControllerAsync, "GetHolidayFlag [datetime] null");
                    return ApiConstant.HOLIDAY_FALSE;
                }
                date = datetime.Date;
            }
            catch (Exception ex)
            {
                _logger.LogError(LoggingEvents.Exception, ex, "GetHolidayFlag [datetime] format error.");
                return ApiConstant.HOLIDAY_FALSE;
            }

            if (context.HolidayMasters.Find(date) != null)
            {
                return ApiConstant.HOLIDAY_TRUE;
            };

            int dayOfWeek = Convert.ToInt32(date.DayOfWeek);
            if (dayOfWeek == 0 || dayOfWeek == 6)
            {
                return ApiConstant.HOLIDAY_TRUE;
            }

            return ApiConstant.HOLIDAY_FALSE;
        }

        /// <summary>
        /// 作業状況一覧作成NGを送信する
        /// </summary>
        /// <param name="socket">要求元</param>
        /// <param name="code">エラーコード</param>
        /// <param name="message">エラーメッセージ</param>
        /// <returns></returns>
        private async Task SendListResultNGAsync(WebSocket socket, string code, string message)
        {
            try
            {
                var apiListrResult = new ApiRegisterResult
                {
                    message_name = ApiConstant.MESSAGE_LIST_RESULT,
                    result = ApiConstant.RESULT_NG,
                    error_code = code,
                    error_message = message
                };
                var json = JsonConvert.SerializeObject(apiListrResult);
                await SendMessageAsync(socket, json);
            }
            catch (Exception ex)
            {
                // 送信後にNGを返すシーケンスはないのでここで終端する
                _logger.LogError(LoggingEvents.Exception, ex, $"作業状況一覧NGの送信に失敗しました。{code} {message}");
            }
        }
    }
}
