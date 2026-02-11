using System.ComponentModel.DataAnnotations;

namespace SignalRDemo.Shared.Models;

/// <summary>
/// 用户模型 - 表示系统中的注册用户
/// </summary>
public class User
{
    /// <summary>
    /// 用户唯一ID
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 用户名（登录用）
    /// </summary>
    [Required]
    [StringLength(20, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 显示昵称
    /// </summary>
    [StringLength(30)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 密码哈希
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 注册时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// 获取显示名称（如果没有设置则返回用户名）
    /// </summary>
    public string GetDisplayName() => !string.IsNullOrEmpty(DisplayName) ? DisplayName : UserName;
}
