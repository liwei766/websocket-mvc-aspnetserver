using GPnaviServer.Data;
using GPnaviServer.Models;
using GPnaviServer.WebSockets;
using GPnaviServer.WebSockets.APIs;
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
        /// DateTimeを文字列化するときのフォーマットプロバイダ
        /// </summary>
        public CultureInfo CultureInfoApi => CultureInfo.CreateSpecificCulture("ja-JP");
        /// <summary>
        /// 装置間IFの日付時刻フォーマット
        /// </summary>
        public string DateTimeFormat => @"yyyy/MM/dd HH:mm:ss.fff";

        /// <summary>
        /// DBコンテキスト
        /// </summary>
        private readonly GPnaviServerContext _context;
        public IotHubApiHandler(IotHubConnectionManager iotHubConnectionManager, GPnaviServerContext dbContext, ILogger<IotHubApiHandler> logger) : base(iotHubConnectionManager, logger)
        {
            _context = dbContext;
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
                var sensorMaster = _context.SensorMasters.FirstOrDefault(e => e.SensorId.Equals(apiSensorEvent.sensor_id));
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
                    _logger.LogError(LoggingEvents.Validation, $"センサーマスタの表示名 {sensorMaster.SensorType} が不正");
                    return;
                }

                _logger.LogTrace(LoggingEvents.IotHubReceive, "突発作業状態を検索");
                var sensorStatus = _context.SensorStatuses.FirstOrDefault(e => e.SensorId.Equals(sensorMaster.SensorId));
                if (sensorStatus != null)
                {
                    _logger.LogTrace(LoggingEvents.IotHubReceive, "前回の発生時刻と比較");
                    var timeSpan = DateTime.Now - sensorStatus.OccurrenceDate;
                    if (timeSpan.TotalMinutes < 1)
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
                    _context.Add(sensorStatus);
                }

                _logger.LogTrace(LoggingEvents.IotHubReceive, "保存する");
                await _context.SaveChangesAsync();

                _logger.LogTrace(LoggingEvents.IotHubReceive, "センサー検知を送信する");
                var apiSensorPush = new ApiSensorPush
                {
                    message_name = ApiConstant.MESSAGE_SENSOR_PUSH,
                    sensor_id = sensorStatus.SensorId,
                    sensor_type = sensorStatus.SensorType,
                    display_message = sensorStatus.DisplayName,
                    date = sensorStatus.OccurrenceDate.ToString(DateTimeFormat, CultureInfoApi)
                };
                var apiSensorPushJson = JsonConvert.SerializeObject(apiSensorPush);

                var androidTokens = _context.UserStatuses.Where(e => e.DeviceType.Equals(ApiConstant.DEVICE_TYPE_ANDROID)).Select(e => e.DeviceToken).ToList();
                if (androidTokens.Count > 0)
                {
                    await PushToNotificationHub.SendNotificationGcmAsync(apiSensorPushJson, androidTokens);
                }
                else
                {
                    _logger.LogTrace(LoggingEvents.IotHubReceive, "Androidデバイスが登録されていない");
                }

                var iotTokens = _context.UserStatuses.Where(e => e.DeviceType.Equals(ApiConstant.DEVICE_TYPE_IOT)).Select(e => e.DeviceToken).ToList();
                if (iotTokens.Count > 0)
                {
                    await PushToNotificationHub.SendNotificationWindowsAsync(apiSensorPushJson, iotTokens);
                }
                else
                {
                    _logger.LogTrace(LoggingEvents.IotHubReceive, "IoTデバイスが登録されていない");
                }

                _logger.LogInformation(LoggingEvents.PushMessageAsync, $"センサー検知の送信に成功しました。{apiSensorPushJson}");
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
