using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking;

public class UdpClientWrapper : IUdpClient, IDisposable
{
    private bool _disposed;
    private readonly IPEndPoint _localEndPoint;
    private CancellationTokenSource? _cts;
    private UdpClient? _udpClient;

    public event EventHandler<byte[]>? MessageReceived;

    public UdpClientWrapper(int port)
    {
        _localEndPoint = new IPEndPoint(IPAddress.Any, port);
    }

    public async Task StartListeningAsync()
    {
        _cts?.Dispose(); 
        _cts = new CancellationTokenSource();
        Console.WriteLine($"UDP: Receiver started on port {_localEndPoint.Port}...");
        try
        {
            _udpClient = new UdpClient(_localEndPoint);
            while (!_cts.Token.IsCancellationRequested)
            {
                UdpReceiveResult result = await _udpClient.ReceiveAsync(_cts.Token);
                MessageReceived?.Invoke(this, result.Buffer);

                Console.WriteLine($"UDP Packet received from remote: {result.RemoteEndPoint}");
            }
        }
        catch (OperationCanceledException)
        {
            //empty
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UDP Error while receiving: {ex.Message}");
        }
    }

    public void StopListening()
    {
        try
        {
            _cts?.Cancel();
            _udpClient?.Close();
            _udpClient?.Dispose();
            Console.WriteLine("UDP: Incoming data listener has been stopped.");
       }
        catch (Exception ex)
        {
            Console.WriteLine($"UDP Termination error: {ex.Message}");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                StopListening();
                _cts?.Dispose();
                _cts = null;
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Exit()
    {
        StopListening();
        Console.WriteLine("UDP Client: Service exited.");
    }
    
public override bool Equals(object? obj)
{
    if (obj is UdpClientWrapper other)
    {
        return _localEndPoint.Equals(other._localEndPoint);
    }
    return false;
}

public override int GetHashCode()
{
    return HashCode.Combine(nameof(UdpClientWrapper), _localEndPoint.Address, _localEndPoint.Port);
}}