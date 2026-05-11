using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServer;

// Додаємо echoService в основний конструктор класу
public class EchoServer(int port, IEchoService echoService)
{
    private readonly int _port = port;
    private readonly IEchoService _echoService = echoService;
    private TcpListener? _listener; // Виправлено: позначено як nullable
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public async Task StartAsync()
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        Console.WriteLine($"Server started on port {_port}.");

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                Console.WriteLine("Client connected.");

                _ = Task.Run(async () => {
                    try {
                        using NetworkStream stream = client.GetStream();
                        await _echoService.HandleStreamAsync(stream, _cancellationTokenSource.Token);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException) {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                    finally {
                        client.Close();
                        Console.WriteLine("Client disconnected.");
                    }
                });
            }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex) { Console.WriteLine($"Listener error: {ex.Message}"); }
        }

        Console.WriteLine("Server shutdown.");
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        _listener?.Stop(); // Виправлено: безпечний виклик через ?
        _cancellationTokenSource.Dispose();
        Console.WriteLine("Server stopped.");
    }

    // Змінено на static async Task, щоб працював await Task.Delay
    public static async Task Main(string[] args)
    {
        EchoServer server = new(5000, new EchoService());
        _ = Task.Run(() => server.StartAsync());

        using var sender = new UdpTimedSender("127.0.0.1", 60000);
        sender.StartSending(5000);

        Console.WriteLine("Press 'q' to quit...");
        while (Console.ReadKey(intercept: true).Key != ConsoleKey.Q) 
        { 
            await Task.Delay(100); 
        }

        sender.StopSending();
        server.Stop();
    }
}

public class UdpTimedSender(string host, int port) : IDisposable
{
    private readonly string _host = host;
    private readonly int _port = port;
    private readonly UdpClient _udpClient = new UdpClient();
    private Timer? _timer; // Виправлено: позначено як nullable

    public void StartSending(int intervalMilliseconds)
    {
        if (_timer != null)
            throw new InvalidOperationException("Sender is already running.");

        _timer = new Timer(SendMessageCallback, null, 0, intervalMilliseconds);
    }

    ushort i = 0;

    private void SendMessageCallback(object? state)
    {
        try
        {
            Random rnd = new();
            byte[] samples = new byte[1024];
            rnd.NextBytes(samples);
            i++;

            byte[] msg = [.. (new byte[] { 0x04, 0x84 }), .. BitConverter.GetBytes(i), .. samples];
            var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);

            _udpClient.Send(msg, msg.Length, endpoint);
            Console.WriteLine($"Message sent to {_host}:{_port} ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }

    public void StopSending()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            StopSending();
            _udpClient?.Dispose();
        }

        _disposed = true;
    }
}