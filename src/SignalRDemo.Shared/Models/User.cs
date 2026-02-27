namespace SignalRDemo.Shared.Models;

/// <summary>
/// 用户
/// </summary>
public class User
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public string GetDisplayName() => string.IsNullOrEmpty(DisplayName) ? UserName : DisplayName;
}
