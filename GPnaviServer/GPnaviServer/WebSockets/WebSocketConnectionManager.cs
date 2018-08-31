using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace GPnaviServer.WebSockets
{
    public class WebSocketConnectionManager
    {
        private ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

        public WebSocket GetSocketById(string id)
        {
            return _sockets.FirstOrDefault(p => p.Key == id).Value;
        }

        public ConcurrentDictionary<string, WebSocket> GetAll()
        {
            return _sockets;
        }

        public string GetId(WebSocket socket)
        {
            return _sockets.FirstOrDefault(p => p.Value == socket).Key;
        }
        public void AddSocket(WebSocket socket)
        {
            var id = CreateConnectionId();
            _sockets.TryAdd(id, socket);

            // セッションキーテーブルに追加する
            _sessionInformations.TryAdd(id, new SessionInformation { Id = id, Socket = socket });
        }

        public async Task RemoveSocket(string id)
        {
            WebSocket socket;
            _sockets.TryRemove(id, out socket);

            // セッションキーテーブルから削除する
            SessionInformation sessionInformation;
            _sessionInformations.TryRemove(id, out sessionInformation);

            await socket.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure,
                                    statusDescription: "Closed by the WebSocketManager",
                                    cancellationToken: CancellationToken.None);
        }

        private string CreateConnectionId()
        {
            return Guid.NewGuid().ToString();
        }
        /// <summary>
        /// セッションキー情報
        /// </summary>
        public class SessionInformation
        {
            /// <summary>
            /// WebSocketのID
            /// </summary>
            public string Id { get; set; }
            /// <summary>
            /// WebSocket本体
            /// </summary>
            public WebSocket Socket { get; set; }
            /// <summary>
            /// 担当者ID
            /// </summary>
            public string LoginId { get; set; }
            /// <summary>
            /// セッションキー
            /// </summary>
            public string SessionKey { get; set; }

        }
        /// <summary>
        /// セッションキーテーブル
        /// </summary>
        private ConcurrentDictionary<string, SessionInformation> _sessionInformations = new ConcurrentDictionary<string, SessionInformation>();
        /// <summary>
        /// 指定されたWebSocketのセッションキーテーブルアイテムを返す
        /// </summary>
        /// <param name="socket">ソケット</param>
        /// <returns>テーブルアイテム</returns>
        public SessionInformation GetSessionInformation(WebSocket socket)
        {
            return _sessionInformations.FirstOrDefault(p => p.Value.Socket == socket).Value;
        }
    }
}
