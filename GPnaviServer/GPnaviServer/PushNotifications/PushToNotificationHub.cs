using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.NotificationHubs;
using Newtonsoft.Json;

namespace GPnaviServer.WebSockets
{
    public class PushToNotificationHub
    {
        /// <summary>
        /// hub name.
        /// </summary>
        private const string HUB_NAME = "gpnavi";
        /// <summary>
        /// connection string with full access.
        /// </summary>
        private const string CONNECTION_STRING = "Endpoint=sb://gpnavi-notifications.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=W9+zC7HrRNShjtUIcvORoAnmmV44BHBOx/aaLvAuzqI=";
        /// <summary>
        /// Android用Push送信
        /// </summary>
        /// <param name="msg">装置間IFのjson文字列</param>
        /// <param name="deviceTokenList">デバイストークン</param>
        /// <returns></returns>
        public static async Task SendNotificationGcmAsync(string msg, List<string> deviceTokenList)
        {
            try
            {
                var notificationJson = "{ \"data\" : " + msg + ",\"android\":{\"priority\":\"high\"}}";
#if TOALL
                // ブロードキャスト
                NotificationHubClient hub = NotificationHubClient.CreateClientFromConnectionString(CONNECTION_STRING, HUB_NAME);
                var notificationOutcome = await hub.SendGcmNativeNotificationAsync(notificationJson);
#else
                // デバイストークン指定
                var notification = new GcmNotification(notificationJson);
                notification.ContentType = "application/json";

                await SendNotificationAsync(notification, deviceTokenList);
#endif
            }
            catch (Exception)
            {
                throw;
            }
        }
        /// <summary>
        /// Azure Function App
        /// </summary>
        private const string FUNCTION_URL = "https://gpnavi-wns-raw.azurewebsites.net/api/HttpTriggerPush?code=mluG82TSSDOEzbUXoc0ayHZk2p/3JYsqEgeCjXbGrq4yAtGAKG07vQ==";
        /// <summary>
        /// Windows IoT Core用Push送信
        /// </summary>
        /// <param name="msg">装置間IFのjson文字列</param>
        /// <param name="deviceTokenList">デバイストークン</param>
        /// <returns></returns>
        public static async Task SendNotificationWindowsAsync(string msg, List<string> deviceTokenList)
        {
            try
            {
#if DIRECT
                // Azure Notification HubsへのPush要求
                //var notification = new WindowsNotification(msg);
                //notification.ContentType = "application/octet-stream";
                //notification.Headers["Content-Type"] = "application/octet-stream";
                //notification.Headers["X-WNS-Type"] = "wns/raw";

                // DEBUG 1 （バッジの例）これだけなら動く
                //var notification = new WindowsNotification("<badge value=\"attention\"/>");

                // DEBUG 2 （トーストの例）これだけなら動く
                var xmlString =
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                    "<toast><visual><binding template = \"ToastText01\">" +
                    "<text id = \"1\">" + msg + "</text>" +
                    "</binding></visual></toast>";
                var notification = new WindowsNotification(xmlString);

                await SendNotificationAsync(notification, deviceTokenList);
#else
                // Azure Function AppへのPush要求
                using (var client = new HttpClient())
                {
                    var content = new StringContent(msg);
                    var httpResponseMessage = await client.PostAsync(FUNCTION_URL, content);
                    System.Diagnostics.Debug.WriteLine($"WNS-RAW FunctionApp STATUS={httpResponseMessage.StatusCode}");
                }
#endif
            }
            catch (Exception)
            {
                throw;
            }
        }
        /// <summary>
        /// Push送信
        /// </summary>
        /// <param name="notification">GCM/WINDOWS用に設定済みの通知</param>
        /// <param name="deviceTokenList">デバイストークン</param>
        /// <returns></returns>
        private static async Task SendNotificationAsync(Notification notification, List<string> deviceTokenList)
        {
            try
            {
                // TODO Define the notification hub.
                NotificationHubClient hub = NotificationHubClient.CreateClientFromConnectionString(CONNECTION_STRING, HUB_NAME);

                //await hub.SendDirectNotificationAsync(notification, deviceTokenList);

                // リストで送信しようとするとtierで例外となるためループ処理する
                foreach (var item in deviceTokenList)
                {
                    var notificationType = (notification is GcmNotification) ? "GCM" : "WNS";
                    System.Diagnostics.Debug.WriteLine($"-- SendNotificationAsync -- {notificationType}");

                    var notificationOutcome = await hub.SendDirectNotificationAsync(notification, item);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// テンプレート登録は使用しない方針
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="deviceTokenList"></param>
        private static async void SendTemplateNotificationAsync(string msg, List<string> deviceTokenList)
        {
            // Define the notification hub.
            NotificationHubClient hub = NotificationHubClient.CreateClientFromConnectionString("<connection string with full access>", "<hub name>");

            // Send the notification as a template notification. All template registrations that contain
            // "messageParam" and the proper tags will receive the notifications.
            // This includes APNS, GCM, WNS, and MPNS template registrations.

            Dictionary<string, string> templateParams = new Dictionary<string, string>();

            foreach (var deviceToken in deviceTokenList)
            {
                templateParams[deviceToken] = msg;
                await hub.SendTemplateNotificationAsync(templateParams);
            }

        }
    }

}
