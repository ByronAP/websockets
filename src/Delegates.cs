using System;
using System.Net.WebSockets;

namespace ByronAP.Net.WebSockets
{
    public delegate void OnMessage(object sender, string message);

    public delegate void OnData(object sender, byte[] data);

    public delegate void OnConnectionOpened(object sender);

    public delegate void OnConnectionClosed(object sender, WebSocketCloseStatus reason);

    public delegate void OnError(object sender, Exception ex);

    public delegate void OnConnectionStateChanged(object sender, WebSocketState newWebSocketState,
        WebSocketState oldWebSocketState);
}
