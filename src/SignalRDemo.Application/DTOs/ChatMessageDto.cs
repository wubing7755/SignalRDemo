using System.Text.Json.Serialization;

namespace SignalRDemo.Application.DTOs;

public class ChatMessageDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    
    [JsonPropertyName("Message")]
    public string Content { get; set; } = string.Empty;
    
    public string Type { get; set; } = "Text";
    public string? MediaUrl { get; set; }
    public string? AltText { get; set; }
    public DateTime Timestamp { get; set; }
}
