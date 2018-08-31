using Microsoft.Extensions.Logging;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace GPnaviServer.WebSockets
{
    /// <summary>
    /// WebSocketハンドラ.
    /// 継承して使用する.
    /// </summary>
    public abstract class WebSocketHandler
    {
        /// <summary>
        /// コネクションマネージャ
        /// </summary>
        protected WebSocketConnectionManager WebSocketConnectionManager { get; set; }
        /// <summary>
        /// ロガー
        /// </summary>
        protected readonly ILogger _logger;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="webSocketConnectionManager">コネクションマネージャ</param>
        public WebSocketHandler(WebSocketConnectionManager webSocketConnectionManager)
        {
            WebSocketConnectionManager = webSocketConnectionManager;
        }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="webSocketConnectionManager">コネクションマネージャ</param>
        /// <param name="logger">ロガー</param>
        public WebSocketHandler(WebSocketConnectionManager webSocketConnectionManager, ILogger<WebSocketHandler> logger)
        {
            WebSocketConnectionManager = webSocketConnectionManager;
            _logger = logger;
        }
        /// <summary>
        /// 接続イベント
        /// </summary>
        /// <param name="socket">接続したソケット</param>
        /// <returns></returns>
        public virtual async Task OnConnected(WebSocket socket)
        {
            _logger?.LogInformation(LoggingEvents.Connect, $"-- CONNECT -- {socket.State}");
            WebSocketConnectionManager.AddSocket(socket);
        }
        /// <summary>
        /// 切断イベント
        /// </summary>
        /// <param name="socket">切断したソケット</param>
        /// <returns></returns>
        public virtual async Task OnDisconnected(WebSocket socket)
        {
            _logger?.LogInformation(LoggingEvents.Connect, $"-- DISCONNECT -- {socket.State}");
            await WebSocketConnectionManager.RemoveSocket(WebSocketConnectionManager.GetId(socket));
        }
        /// <summary>
        /// 送信する
        /// </summary>
        /// <param name="socket">送信先ソケット</param>
        /// <param name="message">文字列</param>
        /// <returns></returns>
        public async Task SendMessageAsync(WebSocket socket, string message)
        {
            _logger?.LogInformation(LoggingEvents.SendMessageAsync, $"-- SEND -- {message}");
            try
            {
                if (socket.State != WebSocketState.Open)
                {
                    _logger?.LogError(LoggingEvents.SendMessageAsync, $"Abort state={socket.State}");
                    return;
                }

                var bytes = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(buffer: new ArraySegment<byte>(array: bytes,
                                                                      offset: 0,
                                                                      count: bytes.Length),
                                       messageType: WebSocketMessageType.Text,
                                       endOfMessage: true,
                                       cancellationToken: CancellationToken.None);
            }
            catch (Exception)
            {
                throw;
            }
        }
        /// <summary>
        /// 送信する
        /// </summary>
        /// <param name="socketId">送信先ソケットID</param>
        /// <param name="message">文字列</param>
        /// <returns></returns>
        public async Task SendMessageAsync(string socketId, string message)
        {
            try
            {
                await SendMessageAsync(WebSocketConnectionManager.GetSocketById(socketId), message);
            }
            catch (Exception)
            {
                throw;
            }
        }
        /// <summary>
        /// 接続中のクライアントすべてに送信する
        /// </summary>
        /// <param name="message">文字列</param>
        /// <returns></returns>
        public async Task SendMessageToAllAsync(string message)
        {
            try
            {
                foreach (var pair in WebSocketConnectionManager.GetAll())
                {
                    if (pair.Value.State == WebSocketState.Open)
                        await SendMessageAsync(pair.Value, message);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 受信イベント
        /// </summary>
        /// <param name="socket">送信元ソケット</param>
        /// <param name="result">受信結果情報</param>
        /// <param name="buffer">受信したバイト配列</param>
        /// <returns></returns>
        public abstract Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer);
        //TODO - decide if exposing the message string is better than exposing the result and buffer


    }
}


