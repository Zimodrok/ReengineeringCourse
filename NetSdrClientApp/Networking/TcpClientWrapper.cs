using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking
{
    public class TcpClientWrapper(string host, int port) : ITcpClient
    {
        private readonly string _host = host;
        private readonly int _port = port;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;

        public bool Connected => _tcpClient != null && _tcpClient.Connected && _stream != null;

        public event EventHandler<byte[]>? MessageReceived;

        public void Connect()
        {
            if (Connected)
            {
                Console.WriteLine($"TCP: Already connected to {_host}:{_port}");
                return;

            }

            _tcpClient = new TcpClient();

            try
            {
                _cts = new CancellationTokenSource();
                _tcpClient.Connect(_host, _port);
                _stream = _tcpClient.GetStream();
                Console.WriteLine($"TCP connection established with {_host}:{_port}");
                _ = StartListeningAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect via TCP: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            if (Connected)
            {
                _cts?.Cancel();
                _stream?.Close();
                _tcpClient?.Close();

                _cts = null;
                _tcpClient = null;
                _stream = null;
                Console.WriteLine("TCP Client successfully disconnected.");
            }
            else
            {
                Console.WriteLine("TCP: No active connection found to disconnect.");
            }
        }

        public async Task SendMessageAsync(byte[] data)
        {
            if (Connected && _stream != null && _stream.CanWrite)
            {
                var hex = data.Select(b => Convert.ToString(b, 16)).Aggregate((l, r) => $"{l} {r}");
                Console.WriteLine($"Message sent: {hex}");
                await _stream.WriteAsync(data);
            }
            else
            {
                throw new InvalidOperationException("Not connected to a server.");
            }
        }

        public async Task SendMessageAsync(string str)
        {
            await SendMessageAsync(Encoding.UTF8.GetBytes(str));
        }

                    var stream = _stream;
            var cts = _cts;

            if (Connected && stream != null && stream.CanRead && cts != null)
            {
                try
                {
                    Console.WriteLine("Starting listening for incoming messages.");

                    while (!cts.Token.IsCancellationRequested)
                    {
                        byte[] buffer = new byte[8194];
                        int bytesRead = await stream.ReadAsync(buffer, cts.Token);
                        if (bytesRead > 0)
                        {
                            MessageReceived?.Invoke(this, buffer.AsSpan(0, bytesRead).ToArray());
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Очікуване скасування при відключенні
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in listening loop: {ex.Message}");
                }
                finally
                {
                    Console.WriteLine("Listener stopped.");
                }
            }
    }
}