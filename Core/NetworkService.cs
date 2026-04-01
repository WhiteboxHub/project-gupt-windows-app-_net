using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

namespace Core;

/// <summary>
/// Provides networking functionality for the remote desktop connection
/// </summary>
public class NetworkService : IDisposable
{
    private TcpClient? _client;
    private TcpListener? _server;
    private NetworkStream? _stream;
    private bool _isConnected;
    private CancellationTokenSource? _cts;

    public event EventHandler<byte[]>? DataReceived;
    public event EventHandler? Disconnected;
    public event EventHandler<string>? StatusChanged;

    public bool IsConnected => _isConnected;

    /// <summary>
    /// Connects to a remote host
    /// </summary>
    public async Task<bool> ConnectAsync(string host, int port)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();
            _isConnected = true;
            StatusChanged?.Invoke(this, "Connected");
            
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveLoop(_cts.Token));
            
            return true;
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Connection failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Starts a server to listen for connections
    /// </summary>
    public async Task<bool> StartServerAsync(int port)
    {
        try
        {
            _server = new TcpListener(IPAddress.Any, port);
            _server.Start();
            StatusChanged?.Invoke(this, $"Listening on port {port}");
            
            _cts = new CancellationTokenSource();
            _ = Task.Run(async () => await AcceptLoop(_cts.Token));
            
            return true;
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Server error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sends raw data
    /// </summary>
    public async Task SendDataAsync(byte[] data)
    {
        if (!_isConnected || _stream == null) return;

        try
        {
            // Send length prefix (4 bytes)
            var length = BitConverter.GetBytes(data.Length);
            await _stream.WriteAsync(length, 0, 4);
            
            // Send data
            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Send error: {ex.Message}");
            Disconnect();
        }
    }

    /// <summary>
    /// Sends a message as string
    /// </summary>
    public async Task SendMessageAsync(string message)
    {
        var data = Encoding.UTF8.GetBytes(message);
        await SendDataAsync(data);
    }

    /// <summary>
    /// Sends serialized object
    /// </summary>
    public async Task SendObjectAsync<T>(T obj)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(obj);
        var data = Encoding.UTF8.GetBytes(json);
        await SendDataAsync(data);
    }

    private async Task AcceptLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _server != null)
        {
            try
            {
                var client = await _server.AcceptTcpClientAsync(ct);
                _client = client;
                _stream = client.GetStream();
                _isConnected = true;
                StatusChanged?.Invoke(this, "Client connected");
                
                _ = Task.Run(() => ReceiveLoop(ct));
                break;
            }
            catch (OperationCanceledException) { break; }
            catch { }
        }
    }

    private async Task ReceiveLoop(CancellationToken ct)
    {
        var buffer = new byte[65536];
        
        while (!ct.IsCancellationRequested && _isConnected && _stream != null)
        {
            try
            {
                // Read length prefix
                var lengthBuffer = new byte[4];
                int bytesRead = await _stream.ReadAsync(lengthBuffer, 0, 4, ct);
                if (bytesRead == 0) break;
                
                int dataLength = BitConverter.ToInt32(lengthBuffer, 0);
                if (dataLength <= 0 || dataLength > 10 * 1024 * 1024) break;
                
                // Read data
                var data = new byte[dataLength];
                int totalRead = 0;
                while (totalRead < dataLength)
                {
                    bytesRead = await _stream.ReadAsync(data, totalRead, dataLength - totalRead, ct);
                    if (bytesRead == 0) break;
                    totalRead += bytesRead;
                }
                
                if (totalRead == dataLength)
                {
                    DataReceived?.Invoke(this, data);
                }
            }
            catch (OperationCanceledException) { break; }
            catch { break; }
        }
        
        Disconnect();
    }

    /// <summary>
    /// Disconnects the current connection
    /// </summary>
    public void Disconnect()
    {
        _isConnected = false;
        _cts?.Cancel();
        
        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }
        try { _server?.Stop(); } catch { }
        
        StatusChanged?.Invoke(this, "Disconnected");
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        Disconnect();
        _cts?.Dispose();
    }
}
