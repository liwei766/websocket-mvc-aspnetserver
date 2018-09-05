using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GPnaviServer.WebSockets
{
    /// <summary>
    /// WebSocketミドルウェア
    /// </summary>
    public class WebSocketManagerMiddleware
    {
        /// <summary>
        /// ロガー
        /// </summary>
        private readonly ILogger _logger;
        /// <summary>
        /// デリゲート
        /// </summary>
        private readonly RequestDelegate _next;
        /// <summary>
        /// ハンドラ
        /// </summary>
        private WebSocketHandler _webSocketHandler { get; set; }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="next">デリゲート</param>
        /// <param name="webSocketHandler">ハンドラ</param>
        public WebSocketManagerMiddleware(RequestDelegate next,
                                          WebSocketHandler webSocketHandler,
                                          ILogger<WebSocketManagerMiddleware> logger)
        {
            _next = next;
            _webSocketHandler = webSocketHandler;
            _logger = logger;
        }
        /// <summary>
        /// ミドルウェア
        /// </summary>
        /// <param name="context">コンテキスト</param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
                return;

            _logger.LogTrace(LoggingEvents.Connect, "WebSocketManagerMiddleware Accept START");
            var socket = await context.WebSockets.AcceptWebSocketAsync();
            await _webSocketHandler.OnConnected(socket);

            _logger.LogTrace(LoggingEvents.Connect, "WebSocketManagerMiddleware Receive START");
            await Receive(socket, async (result, buffer) =>
            {
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    await _webSocketHandler.ReceiveAsync(socket, result, buffer);
                    return;
                }

                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocketHandler.OnDisconnected(socket);
                    return;
                }

            });

            _logger.LogTrace(LoggingEvents.Connect, "WebSocketManagerMiddleware Receive END");

            // investigate the Kestrel exception thrown when this is the last middleware
            //await _next.Invoke(context);
        }
        /// <summary>
        /// 受信ループ
        /// </summary>
        /// <param name="socket">接続されたソケット</param>
        /// <param name="handleMessage">コールバック</param>
        /// <returns></returns>
        private async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            try
            {
                var buffer = new byte[1024 * 4];

                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer),
                                                           cancellationToken: CancellationToken.None);

                    handleMessage(result, buffer);
                }

            }
            catch (WebSocketException ex)
            {
                _logger.LogError(LoggingEvents.Exception, $"Closeなしで切断されたため例外が発生 : {ex.Message}");
            }
            catch(TaskCanceledException ex)
            {
                _logger.LogError(LoggingEvents.Exception, $"サーバ再起動時のキャンセルが発生 : {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(LoggingEvents.Exception, ex, $"WebSocketManagerMiddleware unknown EX : {ex.Message}");
            }
        }

    }
}
