using System;
using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ByronAP.Net.WebSockets
{
    public class WebSocketOptions
    {
        public WebSocketOptions(string url)
        {
            Url = url;
        }

        public WebSocketOptions(string url, ILogger logger)
        {
            Url = url;
            Logger = logger;
        }

        public WebSocketOptions(ILogger logger)
        {
            Logger = logger;
        }

        public ClientWebSocket InnerClientWebSocket { get; } = new();

        public string Url { get; set; }
        public ILogger Logger { get; set; } = new NullLogger<WebSocketClient>();
        public TimeSpan ConnectTimeout { get; set; } = new(0, 0, 0, 20);

        public X509CertificateCollection ClientCertificates => InnerClientWebSocket.Options.ClientCertificates;
        public CookieContainer Cookies => InnerClientWebSocket.Options.Cookies;
        public ICredentials Credentials => InnerClientWebSocket.Options.Credentials;
        public TimeSpan KeepAliveInterval => InnerClientWebSocket.Options.KeepAliveInterval;
        public IWebProxy Proxy => InnerClientWebSocket.Options.Proxy;

        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback =>
            InnerClientWebSocket.Options.RemoteCertificateValidationCallback;

        public bool UseDefaultCredentials => InnerClientWebSocket.Options.UseDefaultCredentials;

        public void AddSubProtocol(string subProtocol)
        {
            InnerClientWebSocket.Options.AddSubProtocol(subProtocol);
        }

        public void SetBuffer(int receiveBufferSize, int sendBufferSize)
        {
            InnerClientWebSocket.Options.SetBuffer(receiveBufferSize, sendBufferSize);
        }

        public void SetBuffer(int receiveBufferSize, int sendBufferSize, ArraySegment<byte> buffer)
        {
            InnerClientWebSocket.Options.SetBuffer(receiveBufferSize, sendBufferSize, buffer);
        }

        public void SetRequestHeader(string headerName, string headerValue)
        {
            InnerClientWebSocket.Options.SetRequestHeader(headerName, headerValue);
        }
    }
}
