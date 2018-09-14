using GPnaviServer.Data;
using GPnaviServer.Models;
using GPnaviServer.WebSockets;
using GPnaviServer.WebSockets.APIs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.IotHub
{
    public class IotHubApiHandler : IotHubHandler
    {
        /// <summary>
        /// センサー区分TRASHのメッセージ
        /// </summary>
        private const string MESSAGE_TRASH = "{0}が一杯になりました。";
        /// <summary>
        /// センサー区分POTのメッセージ
        /// </summary>
        private const string MESSAGE_POT = "{0}に給水してください。";
        /// <summary>
        /// 頻発判定 分単位
        /// </summary>
        private const double SENSOR_TIMESPAN_MINUTES = 1;

        /// <summary>
        /// DateTimeを文字列化するときのフォーマットプロバイダ
        /// </summary>
        public CultureInfo CultureInfoApi => CultureInfo.CreateSpecificCulture("ja-JP");
        /// <summary>
        /// 装置間IFの日付時刻フォーマット
        /// </summary>
        public string DateTimeFormat => @"yyyy/MM/dd HH:mm:ss.fff";
        /// <summary>
        /// センサー区分TRASHのメッセージ
        /// </summary>
        public string MessageTrash { get; }
        /// <summary>
        /// センサー区分POTのメッセージ
        /// </summary>
        public string MessagePot { get; }
        /// <summary>
        /// 頻発判定 分単位
        /// </summary>
        public double SensorTimeSpanMinutes { get; }

        /// <summary>
        /// Notification HUB
        /// </summary>
        public PushToNotificationHub _pushToNotificationHub { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="iotHubConnectionManager">マネージャ</param>
        /// <param name="logger">ロガー</param>
        /// <param name="configuration">設定</param>
        public IotHubApiHandler(IotHubConnectionManager iotHubConnectionManager, ILogger<IotHubApiHandler> logger, IConfiguration configuration) : base(iotHubConnectionManager, logger, configuration)
        {
            var sensorTimeSpanMinutes = Configuration["SensorTimeSpanMinutes"];
            try
            {
                SensorTimeSpanMinutes = double.Parse(sensorTimeSpanMinutes);
            }
            catch (Exception)
            {
                SensorTimeSpanMinutes = SENSOR_TIMESPAN_MINUTES;
            }
            MessageTrash = Configuration["MessageTrash"];
            if (string.IsNullOrWhiteSpace(MessageTrash))
            {
                MessageTrash = MESSAGE_TRASH;
            }
            MessagePot = Configuration["MessagePot"];
            if (string.IsNullOrWhiteSpace(MessagePot))
            {
                MessagePot = MESSAGE_POT;
            }

            _logger.LogWarning(LoggingEvents.Default, ToStringConfig());

            _pushToNotificationHub = new PushToNotificationHub(Configuration);
        }

        protected override async Task OnReceiveAsync(string json)
        {
            try
            {
                _logger.LogInformation(LoggingEvents.IotHubReceive, $"--IOT RECV--{json}");

                _logger.LogTrace(LoggingEvents.IotHubReceive, "パース");
                var apiSensorCommonUp = JsonConvert.DeserializeObject<ApiSensorCommonUp>(json);
                if (apiSensorCommonUp != null && !string.IsNullOrWhiteSpace(apiSensorCommonUp.message_name))
                {
                    _logger.LogTrace(LoggingEvents.IotHubReceive, "受信メッセージ名で分岐");
                    switch (apiSensorCommonUp.message_name)
                    {
                        case ApiConstant.MESSAGE_SENSOR_EVENT:
                            await SensorEventControllerAsync(json);
                            break;

                        default:
                            _logger.LogError(LoggingEvents.ApiFormat, $"装置間IFのメッセージ名 {apiSensorCommonUp.message_name} は存在しません。");
                            break;
                    }
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
        /// センサーイベントコントローラ
        /// </summary>
        /// <param name="json">受信メッセージ</param>
        /// <returns></returns>
        private async Task SensorEventControllerAsync(string json)
        {
            _logger.LogInformation(LoggingEvents.IotHubReceive, "SensorEventControllerAsync START");

            var options = new DbContextOptionsBuilder<GPnaviServerContext>()
                .UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
                .Options;
            using (var context = new GPnaviServerContext(options))
            {
                try
                {
                    _logger.LogTrace(LoggingEvents.IotHubReceive, "パース");
                    var apiSensorEvent = JsonConvert.DeserializeObject<ApiSensorEvent>(json);

                    _logger.LogTrace(LoggingEvents.IotHubReceive, "バリデーションチェック");
                    var (result, message) = IsInvalidApiSensorEvent(apiSensorEvent);
                    if (result)
                    {
                        _logger.LogError(LoggingEvents.Validation, $"バリデーションエラー {message}");
                        return;
                    }

                    _logger.LogTrace(LoggingEvents.IotHubReceive, "センサーマスタ検索");
                    var sensorMaster = context.SensorMasters.FirstOrDefault(e => e.SensorId.Equals(apiSensorEvent.sensor_id));
                    if (sensorMaster == null)
                    {
                        _logger.LogError(LoggingEvents.Validation, $"センサーマスタにセンサーID {apiSensorEvent.sensor_id} が存在しない");
                        return;
                    }

                    _logger.LogTrace(LoggingEvents.IotHubReceive, "センサーマスタ登録内容のバリデーションチェック");
                    if (!(sensorMaster.SensorType.Equals(ApiConstant.SENSOR_TYPE_POT) || sensorMaster.SensorType.Equals(ApiConstant.SENSOR_TYPE_TRASH)))
                    {
                        _logger.LogError(LoggingEvents.Validation, $"センサーマスタのセンサーデバイス区分 {sensorMaster.SensorType} が不正");
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(sensorMaster.DisplayName))
                    {
                        _logger.LogError(LoggingEvents.Validation, $"センサーマスタの表示名 {sensorMaster.DisplayName} が不正");
                        return;
                    }

                    _logger.LogTrace(LoggingEvents.IotHubReceive, "突発作業状態を検索");
                    var sensorStatus = context.SensorStatuses.FirstOrDefault(e => e.SensorId.Equals(sensorMaster.SensorId));
                    if (sensorStatus != null)
                    {
                        _logger.LogTrace(LoggingEvents.IotHubReceive, "前回の発生時刻と比較");
                        var timeSpan = DateTime.Now - sensorStatus.OccurrenceDate;
                        if (timeSpan.TotalMinutes < SensorTimeSpanMinutes)
                        {
                            _logger.LogTrace(LoggingEvents.IotHubReceive, "頻発しているのでイベントを捨てる");
                            return;
                        }
                        else
                        {
                            _logger.LogTrace(LoggingEvents.IotHubReceive, "突発作業発生時刻を更新");
                            sensorStatus.OccurrenceDate = DateTime.Now;
                        }
                    }
                    else
                    {
                        _logger.LogTrace(LoggingEvents.IotHubReceive, "突発作業状態を追加");
                        sensorStatus = new SensorStatus
                        {
                            SensorId = sensorMaster.SensorId,
                            SensorType = sensorMaster.SensorType,
                            DisplayName = sensorMaster.DisplayName,
                            OccurrenceDate = DateTime.Now
                        };
                        context.Add(sensorStatus);
                    }

                    _logger.LogTrace(LoggingEvents.IotHubReceive, "保存する");
                    await context.SaveChangesAsync();

                    _logger.LogTrace(LoggingEvents.IotHubReceive, "センサー検知を送信する");
                    var displayMessage = sensorMaster.SensorType.Equals(ApiConstant.SENSOR_TYPE_TRASH) ? string.Format(MessageTrash, sensorStatus.DisplayName) : string.Format(MessagePot, sensorStatus.DisplayName);
                    var apiSensorPush = new ApiSensorPush
                    {
                        message_name = ApiConstant.MESSAGE_SENSOR_PUSH,
                        sensor_id = sensorStatus.SensorId,
                        sensor_type = sensorStatus.SensorType,
                        display_message = displayMessage,
                        date = sensorStatus.OccurrenceDate.ToString(DateTimeFormat, CultureInfoApi)
                    };
                    var apiSensorPushJson = JsonConvert.SerializeObject(apiSensorPush);

                    var androidTokens = context.UserStatuses.Where(e => e.DeviceType.Equals(ApiConstant.DEVICE_TYPE_ANDROID)).Select(e => e.DeviceToken).ToList();
                    if (androidTokens.Count > 0)
                    {
                        await _pushToNotificationHub.SendNotificationGcmAsync(apiSensorPushJson, androidTokens);
                    }
                    else
                    {
                        _logger.LogWarning(LoggingEvents.IotHubReceive, "Androidデバイスが登録されていない");
                    }

                    var iotTokens = context.UserStatuses.Where(e => e.DeviceType.Equals(ApiConstant.DEVICE_TYPE_IOT)).Select(e => e.DeviceToken).ToList();
                    if (iotTokens.Count > 0)
                    {
                        await _pushToNotificationHub.SendNotificationWindowsAsync(apiSensorPushJson, iotTokens);
                    }
                    else
                    {
                        _logger.LogWarning(LoggingEvents.IotHubReceive, "IoTデバイスが登録されていない");
                    }

                    _logger.LogWarning(LoggingEvents.PushMessageAsync, $"センサー検知の送信に成功しました。{apiSensorPushJson}");
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
                    _logger.LogError(LoggingEvents.Exception, ex, "SensorEventControllerAsync unknown error.");
                }
            }
        }
        /// <summary>
        /// 受信メッセージバリデーション センサーイベント
        /// </summary>
        /// <param name="apiSensorEvent">受信メッセージ</param>
        /// <returns>エラーの場合は真</returns>
        private (bool result, string message) IsInvalidApiSensorEvent(ApiSensorEvent apiSensorEvent)
        {
            if (apiSensorEvent == null)
            {
                return (true, "apiSensorEvent is null.");
            }

            var (result, message) = IsInvalidApiSensorCommonUp(apiSensorEvent);
            if (result)
            {
                return (result, message);
            }

            // センサーイベントに固有部は無い

            return (false, null);
        }
        /// <summary>
        /// 受信メッセージバリデーション 共通部.
        /// </summary>
        /// <param name="apiSensorCommonUp">受信メッセージ</param>
        /// <returns>エラーの場合は真</returns>
        private (bool result, string message) IsInvalidApiSensorCommonUp(ApiSensorCommonUp apiSensorCommonUp)
        {
            if (apiSensorCommonUp == null)
            {
                return (true, "apiSensorCommonUp is null.");
            }

            // message_nameはOnReceiveAsyncでチェック済みなのでここでは不要

            if (string.IsNullOrWhiteSpace(apiSensorCommonUp.sensor_id))
            {
                return (true, "sensor_id is null.");
            }

            return (false, null);
        }
    }
}
