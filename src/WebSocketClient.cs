using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ByronAP.Net.WebSockets
{
    public class WebSocketClient : IDisposable
    {
        private readonly WebSocketOptions _options;
        private readonly SemaphoreSlim _sendSemaphoreSlim = new(1, 1);
        private readonly CancellationTokenSource _tokenSource = new();
#pragma warning disable IDE0052 // Remove unread private members
        private readonly Timer _watchdogTimer;
#pragma warning restore IDE0052 // Remove unread private members
        private bool _disposedValue;
        private WebSocketState _lastWebSocketState = WebSocketState.None;
        private byte[] _receiveBuffer = new byte[1024];
        private Task _receiveTask;

        public WebSocketClient(WebSocketOptions options)
        {
            _options = options;
            _watchdogTimer = new Timer(WatchdogTimerCallback, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(200));
        }

        public Guid InstanceId { get; } = Guid.NewGuid();
        public WebSocketState State => _clientWebSocket.State;

        private ClientWebSocket _clientWebSocket => _options.InnerClientWebSocket;
        private ILogger _logger => _options.Logger;

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void WatchdogTimerCallback(object state)
        {
            if (_lastWebSocketState == _clientWebSocket.State)
                return;

            ConnectionStateChanged?.Invoke(this, _clientWebSocket.State, _lastWebSocketState);

            _lastWebSocketState = _clientWebSocket.State;
        }

        public event OnData DataReceived;
        public event OnMessage MessageReceived;
        public event OnConnectionOpened ConnectionOpened;
        public event OnConnectionClosed ConnectionClosed;
        public event OnError Error;
        public event OnConnectionStateChanged ConnectionStateChanged;

        public async Task DisconnectAsync()
        {
            if (_clientWebSocket.State != WebSocketState.Open)
            {
                _logger.LogWarning("Disconnect called on a closed websocket ({InstanceId}) ({State})", InstanceId,
                    _clientWebSocket.State);
                return;
            }

            _logger.LogInformation("Disconnect called ({InstanceId}", InstanceId);

            await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "normal closure",
                _tokenSource.Token);

            // wait a tad for operations to complete
            await Task.Delay(150);
            //_ = Task.Run(() => ConnectionClosed?.Invoke(this, WebSocketCloseStatus.NormalClosure));
        }

        public async Task<Tuple<bool, Exception>> ConnectAsync()
        {
            _logger.LogInformation("Connect called ({InstanceId}", InstanceId);
            
            // use a stopwatch to keep track of how long we are trying to connect for
            var connStartStopWatch = new Stopwatch();
            connStartStopWatch.Start();
            try
            {
                await _clientWebSocket.ConnectAsync(new Uri(_options.Url), _tokenSource.Token);

                
                
            CHECKSTATE:
                switch (_clientWebSocket.State)
                {
                    case WebSocketState.Connecting:
                        if (connStartStopWatch.Elapsed.TotalSeconds <= _options.ConnectTimeout.TotalSeconds)
                        {
                            // check the state 4 times per second until timeout (arbitrary)
                            await Task.Delay(250);
                            goto CHECKSTATE;
                        }
                        else
                        {
                            // connect timed out
                            try
                            {
                                _clientWebSocket.Abort();
                            }
                            catch
                            {
                                // ignore
                            }

                            var timeoutException = new TimeoutException("WebSocket connection timed out.");
                            _logger.LogError(timeoutException, "WebSocket connect timed out ({InstanceId}).",
                                InstanceId);
                            return new Tuple<bool, Exception>(_clientWebSocket.State == WebSocketState.Open,
                                timeoutException);
                        }
                    case WebSocketState.CloseSent:
                        _logger.LogWarning("WebSocket state is {WSState}", _clientWebSocket.State);
                        break;
                    case WebSocketState.CloseReceived:
                        _logger.LogWarning("WebSocket state is {WSState}", _clientWebSocket.State);
                        break;
                    case WebSocketState.Closed:
                        _logger.LogWarning("WebSocket state is {WSState}", _clientWebSocket.State);
                        break;
                    case WebSocketState.Aborted:
                        _logger.LogWarning("WebSocket state is {WSState}", _clientWebSocket.State);
                        break;
                    default:
                        _logger.LogInformation("WebSocket state is {WSState}", _clientWebSocket.State);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket connect raised an exception.");
                return new Tuple<bool, Exception>(_clientWebSocket.State == WebSocketState.Open, ex);
            }
            finally
            {
                connStartStopWatch.Reset();

                if (_clientWebSocket.State == WebSocketState.Open)
                {
                    ConnectionOpened?.Invoke(this);
                    StartReceiver();
                }
            }

            return new Tuple<bool, Exception>(_clientWebSocket.State == WebSocketState.Open, null);
        }

        public async Task SendTextAsync(string data, EncodingType encodingType = EncodingType.UTF8)
        {
            switch (encodingType)
            {
                case EncodingType.Latin1:
                    await SendTextAsync(Encoding.Latin1.GetBytes(data));
                    break;
                case EncodingType.UTF8:
                    await SendTextAsync(Encoding.UTF8.GetBytes(data));
                    break;
                case EncodingType.UTF32:
                    await SendTextAsync(Encoding.UTF32.GetBytes(data));
                    break;
                case EncodingType.ASCII:
                    await SendTextAsync(Encoding.ASCII.GetBytes(data));
                    break;
                case EncodingType.Unicode:
                    await SendTextAsync(Encoding.Unicode.GetBytes(data));
                    break;
                case EncodingType.BigEndianUnicode:
                    await SendTextAsync(Encoding.BigEndianUnicode.GetBytes(data));
                    break;
                case EncodingType.Default:
                    await SendTextAsync(Encoding.Default.GetBytes(data));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(encodingType));
            }
        }

        public async Task SendTextAsync(byte[] data)
        {
            try
            {
                await _sendSemaphoreSlim.WaitAsync(_tokenSource.Token);

                await _clientWebSocket.SendAsync(data, WebSocketMessageType.Text, true, _tokenSource.Token);
            }
            finally
            {
                _sendSemaphoreSlim.Release();
            }
        }

        public async Task SendBinaryAsync(byte[] data)
        {
            try
            {
                await _sendSemaphoreSlim.WaitAsync(_tokenSource.Token);

                await _clientWebSocket.SendAsync(data, WebSocketMessageType.Binary, true, _tokenSource.Token);
            }
            finally
            {
                _sendSemaphoreSlim.Release();
            }
        }

        public async Task<bool> SendBinaryStreamAsync(Stream dataStream)
        {
            try
            {
                await _sendSemaphoreSlim.WaitAsync(_tokenSource.Token);
                using var sr = new BinaryReader(dataStream);
                var buffer = new byte[1024];
                var bytesRead = 0;
                var count = 0;
                while ((count = sr.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (_clientWebSocket.State != WebSocketState.Open)
                    {
                        _logger.LogError("WebSocket connection closed while sending data ({InstanceId}).", InstanceId);
                        return false;
                    }

                    bytesRead += count;

                    var eof = bytesRead >= dataStream.Length;
                    await _clientWebSocket.SendAsync(buffer, WebSocketMessageType.Binary, eof, _tokenSource.Token);
                    Array.Clear(buffer, 0, buffer.Length);
                }

                buffer = null;
                return true;
            }
            finally
            {
                _sendSemaphoreSlim.Release();
            }
        }

        private void StartReceiver()
        {
            _logger.LogInformation("WebSocket receive task starting.");

            _receiveTask = Task.Run(async () =>
            {
                try
                {
                    while (_clientWebSocket.State == WebSocketState.Open &&
                           !_tokenSource.IsCancellationRequested &&
                           !_disposedValue)
                    {
                        var textMessage = "";
                        var binaryData = new List<byte>();

                    READDATA:
                        var receiveResult = await ReadSocketData();

                        if (receiveResult.Count > 0)
                            switch (receiveResult.MessageType)
                            {
                                case WebSocketMessageType.Text:
                                    textMessage += Encoding.UTF8.GetString(_receiveBuffer).TrimEnd('\0');
                                    if (receiveResult.EndOfMessage)
                                        _ = Task.Run(() => MessageReceived?.Invoke(this, textMessage));
                                    else
                                        goto READDATA;

                                    break;
                                case WebSocketMessageType.Binary:
                                    binaryData.AddRange(_receiveBuffer[new Range(0, receiveResult.Count)]);
                                    if (receiveResult.EndOfMessage)
                                        _ = Task.Run(() => DataReceived?.Invoke(this, binaryData.ToArray()));
                                    else
                                        goto READDATA;

                                    break;
                                case WebSocketMessageType.Close:
                                    // close message should not be seen here since the byte count should be 0 during close
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                        if (receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            try
                            {
                                if (_clientWebSocket.State == WebSocketState.Open)
                                    await DisconnectAsync();
                            }
                            catch
                            {
                                // ignore
                            }

                            _ = Task.Run(() => ConnectionClosed?.Invoke(this, WebSocketCloseStatus.NormalClosure));
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WebSocket receive raised an exception ({InstanceId}).", InstanceId);
                    if (_clientWebSocket.State != WebSocketState.Open && !_tokenSource.IsCancellationRequested &&
                        !_disposedValue)
                    {
                        _ = Task.Run(() => ConnectionClosed?.Invoke(this, WebSocketCloseStatus.InternalServerError));
                        _ = Task.Run(() => Error?.Invoke(this, ex));
                    }
                }
                finally
                {
                    _logger.LogInformation("WebSocket receiver stopped ({InstanceId})", InstanceId);
                }
            });
        }

        private ValueTask<ValueWebSocketReceiveResult> ReadSocketData()
        {
            Array.Clear(_receiveBuffer, 0, _receiveBuffer.Length);
            return _clientWebSocket.ReceiveAsync(new Memory<byte>(_receiveBuffer), _tokenSource.Token);
        }

        protected virtual void Dispose(bool disposing)
        {
            // gives a chance to complete pending tasks
            Task.Delay(250).Wait();

            _logger.LogDebug("WebSocket {InstanceId} is being disposed.", InstanceId);

            if (!_disposedValue)
            {
                if (disposing)
                {
                    var preDisposeWebsocketState = State;

                    _tokenSource.Cancel(false);

                    try
                    {
                        _receiveTask.Dispose();
                    }
                    catch
                    {
                        // ignore
                    }

                    try
                    {
                        if (_clientWebSocket.State == WebSocketState.Connecting ||
                            _clientWebSocket.State == WebSocketState.Open)
                            _clientWebSocket.Abort();

                        if (preDisposeWebsocketState == WebSocketState.Open)
                            _ = Task.Run(() => ConnectionClosed?.Invoke(this, WebSocketCloseStatus.NormalClosure));

                        _clientWebSocket.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Exception raised while disposing of inner client websocket ({InstanceId}).", InstanceId);
                    }
                }

                _receiveBuffer = null;

                _disposedValue = true;
            }
        }
    }
}
