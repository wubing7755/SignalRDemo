using System.ComponentModel.DataAnnotations;

namespace SignalRDemo.Shared.Models;

/// <summary>
/// 聊天房间模型 - 表示一个聊天室
/// </summary>
public class ChatRoom
{
    /// <summary>
    /// 房间唯一ID
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 房间名称
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 房间描述
    /// </summary>
    [StringLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// 创建者用户ID
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// 是否公共房间
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// 房间密码（已哈希）
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 当前成员数
    /// </summary>
    public int MemberCount { get; set; } = 0;
}
