# IoT HUB デバイスイベント発生ツール

## プロジェクト

simulated-device.csproj

## 試験目的に応じてカスタマイズする箇所

### センサーID

SimulatedDevice.cs

                // Create JSON message
                var sensorEvent = new
                {
                    message_name = "EVENT",
                    sensor_id = "9001"
                };

センサーマスタには9001～9004までSeedData.csで登録済み。

### 繰り返し設定

SimulatedDevice.cs

                await Task.Delay(1000);

1秒間隔でループしているので頻発状態となる。

## 9001で1秒ループするバイナリ

simulated-device.win-x86.zip
