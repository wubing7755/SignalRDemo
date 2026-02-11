using System.ComponentModel.DataAnnotations;

namespace SignalRDemo.Shared.Models;

/// <summary>
/// 用户房间关联模型 - 表示用户加入房间的记录
/// </summary>
public class UserRoom
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 房间ID
    /// </summary>
    public string RoomId { get; set; } = string.Empty;

    /// <summary>
    /// 加入时间
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 用户在房间中的角色
    /// </summary>
    public RoomRole Role { get; set; } = RoomRole.Member;
}

/// <summary>
/// 用户在房间中的角色
/// </summary>
public enum RoomRole
{
    /// <summary>
    /// 房间所有者
    /// </summary>
    Owner = 0,

    /// <summary>
    /// 管理员
    /// </summary>
    Admin = 1,

    /// <summary>
    /// 普通成员
    /// </summary>
    Member = 2
}
