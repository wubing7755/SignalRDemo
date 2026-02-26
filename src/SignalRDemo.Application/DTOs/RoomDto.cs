namespace SignalRDemo.Application.DTOs;

public class RoomDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
}
