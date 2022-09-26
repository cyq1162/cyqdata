using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;


namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// 兼容和DotNetCore同步。
    /// </summary>
    public class WebSocketManager
    {

        //
        // 摘要:
        //     Gets a value indicating whether the request is a WebSocket establishment request.
        public bool IsWebSocketRequest
        {
            get
            {

                return false;
            }
        }

        //
        // 摘要:
        //     Gets the list of requested WebSocket sub-protocols.
        public IList<string> WebSocketRequestedProtocols
        {
            get
            {
                return null;
            }
        }

        //
        // 摘要:
        //     Transitions the request to a WebSocket connection.
        //
        // 返回结果:
        //     A task representing the completion of the transition.
        public virtual Task<WebSocket> AcceptWebSocketAsync()
        {
            return AcceptWebSocketAsync(null);
        }

        //
        // 摘要:
        //     Transitions the request to a WebSocket connection using the specified sub-protocol.
        //
        // 参数:
        //   subProtocol:
        //     The sub-protocol to use.
        //
        // 返回结果:
        //     A task representing the completion of the transition.
        public Task<WebSocket> AcceptWebSocketAsync(string subProtocol)
        {
            return null;
        }
    }
}
