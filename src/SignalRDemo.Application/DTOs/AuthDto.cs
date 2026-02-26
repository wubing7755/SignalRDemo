namespace SignalRDemo.Application.DTOs;

public class LoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public UserDto? User { get; set; }
    public string? Token { get; set; }
}

public class JoinRoomResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public RoomDto? Room { get; set; }
}

public class RegisterRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

public class LoginRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class CreateRoomRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = true;
    public string? Password { get; set; }
}

public class JoinRoomRequest
{
    public string RoomId { get; set; } = string.Empty;
    public string? Password { get; set; }
}

public class SendMessageRequest
{
    public string RoomId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Type { get; set; }
    public string? MediaUrl { get; set; }
    public string? AltText { get; set; }
}
