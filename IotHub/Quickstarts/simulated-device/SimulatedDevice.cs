// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This application uses the Azure IoT Hub device SDK for .NET
// For samples see: https://github.com/Azure/azure-iot-sdk-csharp/tree/master/iothub/device/samples

using System;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace simulated_device
{
    class SimulatedDevice
    {
        private static DeviceClient s_deviceClient;

        // The device connection string to authenticate the device with your IoT hub.
        // Using the Azure CLI:
        // az iot hub device-identity show-connection-string --hub-name {YourIoTHubName} --device-id MyDotnetDevice --output table
        private readonly static string s_connectionString_org = "HostName=gp-navi-notification.azure-devices.net;DeviceId=TestPot1;SharedAccessKey=Unj1jrePQpjGg0xP+9ImH+CusB1JG0l58UPmKkdVmvU=";
        private readonly static string s_connectionString_dev = "HostName=gp-navi-notification-dev.azure-devices.net;DeviceId=TestPot1;SharedAccessKey=e1Jyl2GAdr5u9CaeB7TVIEj4ZrqAsVnCgFQBupxwt8o=";
        private readonly static string s_connectionString_stg = "HostName=gp-navi-notification-stg.azure-devices.net;DeviceId=TestPot1;SharedAccessKey=LdeB553jYiPLazbPmvWOzLl2xgsPXRcOWdfA9Frkj9A=";

        // Async method to send simulated telemetry
        private static async void SendDeviceToCloudMessagesAsync(string sensorId, bool loop)
        {
#if SAMPLE
            // Initial telemetry values
            double minTemperature = 20;
            double minHumidity = 60;
            Random rand = new Random();
#endif

            while (true)
            {
#if SAMPLE
                double currentTemperature = minTemperature + rand.NextDouble() * 15;
                double currentHumidity = minHumidity + rand.NextDouble() * 20;

                // Create JSON message
                var telemetryDataPoint = new
                {
                    temperature = currentTemperature,
                    humidity = currentHumidity
                };
                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

#else
                // Create JSON message
                var sensorEvent = new
                {
                    message_name = "EVENT",
                    sensor_id = sensorId
                };
                var messageString = JsonConvert.SerializeObject(sensorEvent);
                var message = new Message(Encoding.UTF8.GetBytes(messageString));
#endif

                // Add a custom application property to the message.
                // An IoT hub can filter on these properties without access to the message body.
                //message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

                // Send the tlemetry message
                await s_deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

                await Task.Delay(1000);

                if (!loop)
                {
                    Console.WriteLine("送信完了");
                    break;
                }
            }
        }
        private static void Main(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                Name = "simulated_device"
            };

            app.HelpOption("-?|-h|--help");

            var idArgument = app.Argument(
                name: "sensorId",
                description: "Sensor ID",
                multipleValues: false);

            var devOption = app.Option(
                template: "-dev",
                description: "dev環境",
                optionType: CommandOptionType.NoValue);

            var stgOption = app.Option(
                template: "-stg",
                description: "stg環境",
                optionType: CommandOptionType.NoValue);

            var loopOption = app.Option(
                template: "-loop",
                description: "繰り返し実行",
                optionType: CommandOptionType.NoValue);

            app.OnExecute(() =>
            {
                var s_connectionString = s_connectionString_org;

                if (devOption.HasValue())
                {
                    Console.WriteLine("dev環境");
                    s_connectionString = s_connectionString_dev;
                }
                else if (stgOption.HasValue())
                {
                    Console.WriteLine("stg環境");
                    s_connectionString = s_connectionString_stg;
                }
                else
                {
                    Console.WriteLine("デフォルト環境");
                }

                string sensorId = "9001";

                if(idArgument.Value != null)
                {
                    sensorId = idArgument.Value;
                }

                bool loop = false;

                if (loopOption.HasValue())
                {
                    loop = true;
                    Console.WriteLine("ループ実行");
                }
                else
                {
                    Console.WriteLine("一回だけ実行");
                }

                Console.WriteLine("IoT Hub Quickstarts #1 - Simulated device. Ctrl-C to exit.\n");

                // Connect to the IoT hub using the MQTT protocol
                s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, TransportType.Mqtt);
                SendDeviceToCloudMessagesAsync(sensorId, loop);
                Console.ReadLine();

                return 0;
            });

            app.Execute(args);
        }
    }
}
