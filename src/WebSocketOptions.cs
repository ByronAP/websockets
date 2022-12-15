using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;

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

        public ClientWebSocket InnerClientWebSocket { get; } = new ClientWebSocket();

        public string Url { get; set; }
        public ILogger Logger { get; set; } = new NullLogger<WebSocketClient>();
        public TimeSpan ConnectTimeout { get; set; } = new TimeSpan(0, 0, 0, 20);

        public X509CertificateCollection ClientCertificates
        {
            get
            {
                return InnerClientWebSocket.Options.ClientCertificates;
            }
            set
            {
                InnerClientWebSocket.Options.ClientCertificates = value;
            }
        }
        public CookieContainer Cookies
        {
            get
            {
                return InnerClientWebSocket.Options.Cookies;
            }
            set
            {
                InnerClientWebSocket.Options.Cookies = value;
            }
        }
        public ICredentials Credentials
        {
            get
            {
                return InnerClientWebSocket.Options.Credentials;
            }
            set
            {
                InnerClientWebSocket.Options.Credentials = value;
            }
        }
        public TimeSpan KeepAliveInterval
        {
            get
            {
                return InnerClientWebSocket.Options.KeepAliveInterval;
            }
            set
            {
                InnerClientWebSocket.Options.KeepAliveInterval = value;
            }
        }
        public IWebProxy Proxy
        {
            get
            {
                return InnerClientWebSocket.Options.Proxy;
            }
            set
            {
                InnerClientWebSocket.Options.Proxy = value;
            }
        }
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback
        {
            get
            {
                return InnerClientWebSocket.Options.RemoteCertificateValidationCallback;
            }
            set
            {
                InnerClientWebSocket.Options.RemoteCertificateValidationCallback = value;
            }
        }
        public bool UseDefaultCredentials
        {
            get
            {
                return InnerClientWebSocket.Options.UseDefaultCredentials;
            }
            set
            {
                InnerClientWebSocket.Options.UseDefaultCredentials = value;
            }
        }

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
