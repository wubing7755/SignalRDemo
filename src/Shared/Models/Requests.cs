using System.ComponentModel.DataAnnotations;

namespace SignalRDemo.Shared.Models;

/// <summary>
/// 注册请求
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required]
    [StringLength(20, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 显示昵称（可选）
    /// </summary>
    [StringLength(30)]
    public string? DisplayName { get; set; }
}

/// <summary>
/// 登录请求
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 用户名或邮箱
    /// </summary>
    [Required]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 登录响应
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 用户信息
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// JWT 访问令牌
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string? RefreshToken { get; set; }
}

/// <summary>
/// 创建房间请求
/// </summary>
public class CreateRoomRequest
{
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
    /// 是否公共房间
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// 房间密码（私人房间必填）
    /// </summary>
    [StringLength(50)]
    public string? Password { get; set; }
}

/// <summary>
/// 加入房间请求
/// </summary>
public class JoinRoomRequest
{
    /// <summary>
    /// 房间ID
    /// </summary>
    [Required]
    public string RoomId { get; set; } = string.Empty;

    /// <summary>
    /// 房间密码（私人房间需要）
    /// </summary>
    public string? Password { get; set; }
}

/// <summary>
/// 加入房间响应
/// </summary>
public class JoinRoomResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 房间信息
    /// </summary>
    public ChatRoom? Room { get; set; }
}

/// <summary>
/// 发送消息请求
/// </summary>
public class SendMessageRequest
{
    /// <summary>
    /// 房间ID
    /// </summary>
    [Required]
    public string RoomId { get; set; } = string.Empty;

    /// <summary>
    /// 消息内容
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 消息类型
    /// </summary>
    public MessageType Type { get; set; } = MessageType.Text;

    /// <summary>
    /// 媒体URL（图片、文件等）
    /// </summary>
    public string? MediaUrl { get; set; }

    /// <summary>
    /// 替代文本
    /// </summary>
    public string? AltText { get; set; }
}
