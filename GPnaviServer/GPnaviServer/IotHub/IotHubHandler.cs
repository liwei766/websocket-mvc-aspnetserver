using GPnaviServer.WebSockets;
using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GPnaviServer.IotHub
{
    /// <summary>
    /// IOT HUB ハンドラ
    /// </summary>
    public abstract class IotHubHandler
    {
        /// <summary>
        /// IoT HUB 接続マネージャ
        /// </summary>
        protected readonly IotHubConnectionManager _iotHubConnectionManager;
        /// <summary>
        /// ロガー
        /// </summary>
        protected readonly ILogger _logger;
        /// <summary>
        /// 設定
        /// </summary>
        public IConfiguration Configuration { get; }

        public string EventHubsCompatibleEndpoint { get; }
        public string EventHubsCompatiblePath { get; }
        public string IotHubSasKey { get; }
        public string IotHubSasKeyName { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="iotHubConnectionManager">IoT HUB 接続マネージャ</param>
        /// <param name="logger">ロガー</param>
        public IotHubHandler(IotHubConnectionManager iotHubConnectionManager, ILogger logger, IConfiguration configuration)
        {
            _iotHubConnectionManager = iotHubConnectionManager;
            _logger = logger;
            Configuration = configuration;

            EventHubsCompatibleEndpoint = Configuration["EventHubsCompatibleEndpoint"] ?? s_eventHubsCompatibleEndpoint;
            EventHubsCompatiblePath = Configuration["EventHubsCompatiblePath"] ?? s_eventHubsCompatiblePath;
            IotHubSasKey = Configuration["IotHubSasKey"] ?? s_iotHubSasKey;
            IotHubSasKeyName = Configuration["IotHubSasKeyName"] ?? s_iotHubSasKeyName;
        }
        /// <summary>
        /// 設定情報取得
        /// </summary>
        /// <returns>設定情報の文字列</returns>
        public string ToStringConfig()
        {
            return $"{nameof(EventHubsCompatibleEndpoint)}:{EventHubsCompatibleEndpoint} / " +
                $"{nameof(EventHubsCompatiblePath)}:{EventHubsCompatiblePath} / " +
                $"{nameof(IotHubSasKey)}:{IotHubSasKey}" +
                $"{nameof(IotHubSasKeyName)}:{IotHubSasKeyName}";
        }
        /// <summary>
        /// 接続中であれば真
        /// </summary>
        public bool IsConnect
        {
            get { return _iotHubConnectionManager.IsConnect; }
        }
        /// <summary>
        /// 接続する
        /// </summary>
        public void Connect()
        {
            _logger.LogWarning(LoggingEvents.IotHubReceive, $"--IOT CONNECT--");

            // 接続中にする
            _iotHubConnectionManager.IsConnect = true;

            IotMainAsync();
        }
        /// <summary>
        /// 受信イベントハンドラ.
        /// 継承したクラスで実装する.
        /// </summary>
        /// <param name="json">受信メッセージjson文字列</param>
        /// <returns></returns>
        protected abstract Task OnReceiveAsync(string json);

        private readonly static string s_eventHubsCompatibleEndpoint = "sb://iothub-ns-gp-navi-no-659874-48d37046fc.servicebus.windows.net/";
        private readonly static string s_eventHubsCompatiblePath = "gp-navi-notification";
        private readonly static string s_iotHubSasKey = "0G32NnZR1AgmU7SOMKMzHqZknFr/3po1TQEGeQ0cFK4=";
        private readonly static string s_iotHubSasKeyName = "iothubowner";
        private static EventHubClient s_eventHubClient;

        private async void IotMainAsync()
        {
            try
            {
                // Create an EventHubClient instance to connect to the
                // IoT Hub Event Hubs-compatible endpoint.
                var connectionString = new EventHubsConnectionStringBuilder(new Uri(EventHubsCompatibleEndpoint), EventHubsCompatiblePath, IotHubSasKeyName, IotHubSasKey);
                s_eventHubClient = EventHubClient.CreateFromConnectionString(connectionString.ToString());

                // Create a PartitionReciever for each partition on the hub.
                var runtimeInfo = await s_eventHubClient.GetRuntimeInformationAsync();
                var d2cPartitions = runtimeInfo.PartitionIds;

                CancellationTokenSource cts = new CancellationTokenSource();

                var tasks = new List<Task>();
                foreach (string partition in d2cPartitions)
                {
                    tasks.Add(ReceiveMessagesFromDeviceAsync(partition, cts.Token));
                }

                // Wait for all the PartitionReceivers to finsih.
                Task.WaitAll(tasks.ToArray());

                // 接続中状態を解除する
                _iotHubConnectionManager.IsConnect = false;

            }
            catch (Exception ex)
            {
                _logger.LogError(LoggingEvents.Exception, ex, $"IoT HUB exception : {ex.Message}");
                // 接続中状態を解除する
                _iotHubConnectionManager.IsConnect = false;
            }
        }
        private async Task ReceiveMessagesFromDeviceAsync(string partition, CancellationToken ct)
        {
            try
            {
                // Create the receiver using the default consumer group.
                // For the purposes of this sample, read only messages sent since 
                // the time the receiver is created. Typically, you don't want to skip any messages.
                var eventHubReceiver = s_eventHubClient.CreateReceiver("$Default", partition, EventPosition.FromEnqueuedTime(DateTime.Now));
                //Console.WriteLine("Create receiver on partition: " + partition);
                while (true)
                {
                    if (ct.IsCancellationRequested) break;
                    //Console.WriteLine("Listening for messages on: " + partition);
                    // Check for EventData - this methods times out if there is nothing to retrieve.
                    var events = await eventHubReceiver.ReceiveAsync(100);

                    // If there is data in the batch, process it.
                    if (events == null) continue;

                    foreach (EventData eventData in events)
                    {
                        string data = Encoding.UTF8.GetString(eventData.Body.Array);
                        //Console.WriteLine("Message received on partition {0}:", partition);
                        //Console.WriteLine("  {0}:", data);
                        //Console.WriteLine("Application properties (set by device):");
                        //foreach (var prop in eventData.Properties)
                        //{
                        //    Console.WriteLine("  {0}: {1}", prop.Key, prop.Value);
                        //}
                        //Console.WriteLine("System properties (set by IoT Hub):");
                        //foreach (var prop in eventData.SystemProperties)
                        //{
                        //    Console.WriteLine("  {0}: {1}", prop.Key, prop.Value);
                        //}

                        await OnReceiveAsync(data);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
