namespace SignalRDemo.Shared.Models;

/// <summary>
/// 登录请求
/// </summary>
public class LoginRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 注册请求
/// </summary>
public class RegisterRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// 创建房间请求
/// </summary>
public class CreateRoomRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = true;
    public string? Password { get; set; }
}

/// <summary>
/// 加入房间请求
/// </summary>
public class JoinRoomRequest
{
    public string RoomId { get; set; } = string.Empty;
    public string? Password { get; set; }
}

/// <summary>
/// 发送消息请求
/// </summary>
public class SendMessageRequest
{
    public string RoomId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public string TypeString => Type.ToString(); // 兼容字符串类型
    public string? MediaUrl { get; set; }
    public string? AltText { get; set; }
}
