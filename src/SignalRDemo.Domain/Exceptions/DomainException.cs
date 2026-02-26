namespace SignalRDemo.Domain.Exceptions;

public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string message) : base(message)
    {
        Code = "DOMAIN_ERROR";
    }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}

public class UserAlreadyExistsException : DomainException
{
    public UserAlreadyExistsException(string userName) 
        : base("USER_ALREADY_EXISTS", $"用户名 '{userName}' 已存在") { }
}

public class InvalidPasswordException : DomainException
{
    public InvalidPasswordException() 
        : base("INVALID_PASSWORD", "用户名或密码错误") { }
}

public class UserNotFoundException : DomainException
{
    public UserNotFoundException(string userId) 
        : base("USER_NOT_FOUND", $"用户不存在: {userId}") { }
}

public class RoomNotFoundException : DomainException
{
    public RoomNotFoundException(string roomId) 
        : base("ROOM_NOT_FOUND", $"房间不存在: {roomId}") { }
}

public class RoomAlreadyExistsException : DomainException
{
    public RoomAlreadyExistsException(string roomName) 
        : base("ROOM_ALREADY_EXISTS", $"房间 '{roomName}' 已存在") { }
}

public class InvalidRoomPasswordException : DomainException
{
    public InvalidRoomPasswordException() 
        : base("INVALID_ROOM_PASSWORD", "房间密码错误") { }
}

public class UserNotInRoomException : DomainException
{
    public UserNotInRoomException(string userId, string roomId) 
        : base("USER_NOT_IN_ROOM", $"用户不在房间中") { }
}
