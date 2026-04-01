using System.Text.Json.Serialization;

namespace Shared;

/// <summary>
/// Signaling message types
/// </summary>
public enum SignalingMessageType
{
    HostRegistered = 1,
    ClientRegistered = 2,
    SessionRequest = 3,
    SessionAccepted = 4,
    SessionRejected = 5,
    HostDisconnected = 6,
    ClientDisconnected = 7,
    Chat = 8
}

/// <summary>
/// Base signaling message
/// </summary>
public abstract class SignalingMessage
{
    [JsonPropertyName("type")]
    public SignalingMessageType Type { get; set; }
    
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;
    
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

/// <summary>
/// Host registered message
/// </summary>
public class HostRegisteredMessage : SignalingMessage
{
    public HostRegisteredMessage() { Type = SignalingMessageType.HostRegistered; }
    
    [JsonPropertyName("hostName")]
    public string HostName { get; set; } = string.Empty;
    
    [JsonPropertyName("screenWidth")]
    public int ScreenWidth { get; set; }
    
    [JsonPropertyName("screenHeight")]
    public int ScreenHeight { get; set; }
    
    [JsonPropertyName("hostAddress")]
    public string HostAddress { get; set; } = string.Empty;
    
    [JsonPropertyName("hostPort")]
    public int HostPort { get; set; }
}

/// <summary>
/// Client registered message
/// </summary>
public class ClientRegisteredMessage : SignalingMessage
{
    public ClientRegisteredMessage() { Type = SignalingMessageType.ClientRegistered; }
    
    [JsonPropertyName("clientName")]
    public string ClientName { get; set; } = string.Empty;
}

/// <summary>
/// Session request message
/// </summary>
public class SessionRequestMessage : SignalingMessage
{
    public SessionRequestMessage() { Type = SignalingMessageType.SessionRequest; }
    
    [JsonPropertyName("clientAddress")]
    public string ClientAddress { get; set; } = string.Empty;
}

/// <summary>
/// Session accepted message
/// </summary>
public class SessionAcceptedMessage : SignalingMessage
{
    public SessionAcceptedMessage() { Type = SignalingMessageType.SessionAccepted; }
    
    [JsonPropertyName("hostAddress")]
    public string HostAddress { get; set; } = string.Empty;
    
    [JsonPropertyName("hostPort")]
    public int HostPort { get; set; }
}

/// <summary>
/// Session rejected message
/// </summary>
public class SessionRejectedMessage : SignalingMessage
{
    public SessionRejectedMessage() { Type = SignalingMessageType.SessionRejected; }
    
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Disconnected message
/// </summary>
public class DisconnectedMessage : SignalingMessage
{
    public DisconnectedMessage() { Type = SignalingMessageType.HostDisconnected; }
    
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Session info for listing available sessions
/// </summary>
public class SessionInfo
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;
    
    [JsonPropertyName("hostName")]
    public string HostName { get; set; } = string.Empty;
    
    [JsonPropertyName("screenWidth")]
    public int ScreenWidth { get; set; }
    
    [JsonPropertyName("screenHeight")]
    public int ScreenHeight { get; set; }
    
    [JsonPropertyName("hostAddress")]
    public string HostAddress { get; set; } = string.Empty;
    
    [JsonPropertyName("hostPort")]
    public int HostPort { get; set; }
    
    [JsonPropertyName("registeredAt")]
    public long RegisteredAt { get; set; }
}
