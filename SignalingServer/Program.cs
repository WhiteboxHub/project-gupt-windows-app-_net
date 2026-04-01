using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// Add SignalR services
builder.Services.AddSignalR();

// Configure CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

app.UseCors();
app.MapControllers();
app.MapHub<RemoteDesktopHub>("/remoteDesktop");

Console.WriteLine("Signaling Server started on http://localhost:5000");
Console.WriteLine("Hub endpoint: /remoteDesktop");

app.Run();

/// <summary>
/// SignalR Hub for remote desktop signaling
/// </summary>
public class RemoteDesktopHub : Hub
{
    private static readonly ConcurrentDictionary<string, HostConnection> _hosts = new();
    private static readonly ConcurrentDictionary<string, ClientConnection> _clients = new();

    public async Task RegisterHost(string sessionId, string hostName, int screenWidth, int screenHeight, string hostAddress, int hostPort)
    {
        var connectionId = Context.ConnectionId;
        
        var hostInfo = new HostConnection
        {
            SessionId = sessionId,
            HostName = hostName,
            ScreenWidth = screenWidth,
            ScreenHeight = screenHeight,
            HostAddress = hostAddress,
            HostPort = hostPort,
            ConnectionId = connectionId,
            ConnectedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        
        _hosts[sessionId] = hostInfo;
        
        Console.WriteLine($"Host registered: {sessionId} ({hostName}) at {hostAddress}:{hostPort}");
        
        // Notify all clients about new host
        await Clients.All.SendAsync("HostRegistered", new
        {
            sessionId,
            hostName,
            screenWidth,
            screenHeight,
            hostAddress,
            hostPort
        });
    }

    public async Task<List<object>> GetAvailableHosts()
    {
        return _hosts.Values.Select(h => (object)new
        {
            h.SessionId,
            h.HostName,
            h.ScreenWidth,
            h.ScreenHeight,
            h.HostAddress,
            h.HostPort
        }).ToList();
    }

    public async Task<bool> RequestSession(string sessionId, string clientName)
    {
        var clientConnectionId = Context.ConnectionId;
        
        if (_hosts.TryGetValue(sessionId, out var host))
        {
            // Store client info
            _clients[clientConnectionId] = new ClientConnection
            {
                SessionId = sessionId,
                ClientName = clientName,
                ConnectionId = clientConnectionId
            };
            
            Console.WriteLine($"Client '{clientName}' requested session: {sessionId}");
            
            // Notify the host about session request
            await Clients.Client(host.ConnectionId).SendAsync("SessionRequest", new
            {
                sessionId,
                clientName,
                clientConnectionId
            });
            
            return true;
        }
        
        return false;
    }

    public async Task AcceptSession(string clientConnectionId, string sessionId)
    {
        if (_hosts.TryGetValue(sessionId, out var host))
        {
            Console.WriteLine($"Session accepted: {sessionId} for client {clientConnectionId}");
            
            // Notify the client that session is accepted
            await Clients.Client(clientConnectionId).SendAsync("SessionAccepted", new
            {
                sessionId,
                hostAddress = host.HostAddress,
                hostPort = host.HostPort
            });
        }
    }

    public async Task RejectSession(string clientConnectionId, string sessionId, string reason)
    {
        Console.WriteLine($"Session rejected: {sessionId} for client {clientConnectionId} - {reason}");
        
        await Clients.Client(clientConnectionId).SendAsync("SessionRejected", new
        {
            sessionId,
            reason
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        
        // Check if it's a host
        var host = _hosts.FirstOrDefault(h => h.Value.ConnectionId == connectionId);
        if (host.Value != null)
        {
            _hosts.TryRemove(host.Key, out _);
            Console.WriteLine($"Host disconnected: {host.Key}");
            
            // Notify all clients
            await Clients.All.SendAsync("HostDisconnected", new { sessionId = host.Key });
        }
        
        // Check if it's a client
        if (_clients.TryRemove(connectionId, out var client))
        {
            Console.WriteLine($"Client disconnected: {client.ClientName}");
            await Clients.All.SendAsync("ClientDisconnected", new { sessionId = client.SessionId });
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// Information about a connected host
/// </summary>
public class HostConnection
{
    public string SessionId { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }
    public string HostAddress { get; set; } = string.Empty;
    public int HostPort { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
    public long ConnectedAt { get; set; }
}

/// <summary>
/// Information about a connected client
/// </summary>
public class ClientConnection
{
    public string SessionId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
}
