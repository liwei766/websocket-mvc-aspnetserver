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

- simulated-device.publish.zip
  - センサーID 9001
- simulated-device.9003.publish.zip
  - センサーID 9003
- simulated-device.9005.publish.zip
  - センサーID 9005
  - センサーマスタにIDが存在／区分が存在しない
- simulated-device.9006.publish.zip
  - センサーID 9006
  - センサーマスタにIDが存在しない

- simulated-device.multi.publish.zip
  - 環境指定可能
  - センサーID指定可能
  - ループ指定可能

        >simulated-device.exe -h
        
        Usage: simulated_device [arguments] [options]
        
        Arguments:
          sensorId  Sensor ID
        
        Options:
          -?|-h|--help  Show help information
          -dev          dev環境
          -stg          stg環境
          -loop         繰り返し実行
  - 使用例

        >simulated-device.exe -dev -loop 9003
