using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core;

/// <summary>
/// Message types for remote desktop protocol
/// </summary>
public enum MessageType
{
    ScreenFrame = 1,
    MouseMove = 2,
    MouseClick = 3,
    MouseScroll = 4,
    KeyPress = 5,
    TextInput = 6,
    SessionInfo = 7,
    Ping = 8,
    Pong = 9,
    Disconnect = 10
}

/// <summary>
/// Base message class
/// </summary>
public abstract class ProtocolMessage
{
    [JsonPropertyName("type")]
    public MessageType Type { get; set; }
    
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

/// <summary>
/// Screen frame message
/// </summary>
public class ScreenFrameMessage : ProtocolMessage
{
    public ScreenFrameMessage() { Type = MessageType.ScreenFrame; }
    
    [JsonPropertyName("width")]
    public int Width { get; set; }
    
    [JsonPropertyName("height")]
    public int Height { get; set; }
    
    [JsonPropertyName("imageData")]
    public string ImageDataBase64 { get; set; } = string.Empty;
    
    [JsonPropertyName("format")]
    public string Format { get; set; } = "jpeg";
    
    [JsonPropertyName("quality")]
    public int Quality { get; set; } = 70;
}

/// <summary>
/// Mouse move message
/// </summary>
public class MouseMoveMessage : ProtocolMessage
{
    public MouseMoveMessage() { Type = MessageType.MouseMove; }
    
    [JsonPropertyName("x")]
    public int X { get; set; }
    
    [JsonPropertyName("y")]
    public int Y { get; set; }
}

/// <summary>
/// Mouse click message
/// </summary>
public class MouseClickMessage : ProtocolMessage
{
    public MouseClickMessage() { Type = MessageType.MouseClick; }
    
    [JsonPropertyName("x")]
    public int X { get; set; }
    
    [JsonPropertyName("y")]
    public int Y { get; set; }
    
    [JsonPropertyName("button")]
    public string Button { get; set; } = "left";
    
    [JsonPropertyName("isDown")]
    public bool IsDown { get; set; }
}

/// <summary>
/// Mouse scroll message
/// </summary>
public class MouseScrollMessage : ProtocolMessage
{
    public MouseScrollMessage() { Type = MessageType.MouseScroll; }
    
    [JsonPropertyName("delta")]
    public int Delta { get; set; }
}

/// <summary>
/// Key press message
/// </summary>
public class KeyPressMessage : ProtocolMessage
{
    public KeyPressMessage() { Type = MessageType.KeyPress; }
    
    [JsonPropertyName("virtualKey")]
    public int VirtualKey { get; set; }
    
    [JsonPropertyName("isDown")]
    public bool IsDown { get; set; }
}

/// <summary>
/// Text input message
/// </summary>
public class TextInputMessage : ProtocolMessage
{
    public TextInputMessage() { Type = MessageType.TextInput; }
    
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Session info message
/// </summary>
public class SessionInfoMessage : ProtocolMessage
{
    public SessionInfoMessage() { Type = MessageType.SessionInfo; }
    
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;
    
    [JsonPropertyName("hostName")]
    public string HostName { get; set; } = string.Empty;
    
    [JsonPropertyName("screenWidth")]
    public int ScreenWidth { get; set; }
    
    [JsonPropertyName("screenHeight")]
    public int ScreenHeight { get; set; }
}

/// <summary>
/// Protocol handler for serializing/deserializing messages
/// </summary>
public class ProtocolHandler
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a message to JSON bytes
    /// </summary>
    public static byte[] Serialize(ProtocolMessage message)
    {
        var json = JsonSerializer.Serialize(message, message.GetType(), Options);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Deserializes JSON bytes to a message
    /// </summary>
    public static ProtocolMessage? Deserialize(byte[] data)
    {
        var json = System.Text.Encoding.UTF8.GetString(data);
        using var doc = JsonDocument.Parse(json);
        var type = doc.RootElement.GetProperty("type").GetInt32();
        
        return type switch
        {
            1 => JsonSerializer.Deserialize<ScreenFrameMessage>(json, Options),
            2 => JsonSerializer.Deserialize<MouseMoveMessage>(json, Options),
            3 => JsonSerializer.Deserialize<MouseClickMessage>(json, Options),
            4 => JsonSerializer.Deserialize<MouseScrollMessage>(json, Options),
            5 => JsonSerializer.Deserialize<KeyPressMessage>(json, Options),
            6 => JsonSerializer.Deserialize<TextInputMessage>(json, Options),
            7 => JsonSerializer.Deserialize<SessionInfoMessage>(json, Options),
            8 or 9 or 10 => JsonSerializer.Deserialize<ProtocolMessage>(json, Options),
            _ => null
        };
    }
}
