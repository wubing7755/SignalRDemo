namespace SignalRDemo.Shared.Models;

/// <summary>
/// 登录响应
/// </summary>
public class LoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public User? User { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
}

/// <summary>
/// 注册响应
/// </summary>
public class RegisterResponse : LoginResponse { }

/// <summary>
/// 创建房间响应
/// </summary>
public class CreateRoomResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ChatRoom? Room { get; set; }
}

/// <summary>
/// 加入房间响应
/// </summary>
public class JoinRoomResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ChatRoom? Room { get; set; }
}
