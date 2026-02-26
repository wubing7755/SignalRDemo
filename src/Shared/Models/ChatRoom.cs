namespace SignalRDemo.Shared.Models;

/// <summary>
/// 聊天室
/// </summary>
public class ChatRoom
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public string? Password { get; set; }
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
