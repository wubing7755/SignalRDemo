using SignalRDemo.Shared.Models;

namespace SignalRDemo.Server.Services;

/// <summary>
/// 房间服务接口
/// </summary>
public interface IRoomService
{
    /// <summary>
    /// 创建房间
    /// </summary>
    Task<ChatRoom> CreateRoomAsync(string name, string? description, string ownerId, bool isPublic, string? password);

    /// <summary>
    /// 获取所有公共房间
    /// </summary>
    Task<List<ChatRoom>> GetPublicRoomsAsync();

    /// <summary>
    /// 获取用户加入的所有房间
    /// </summary>
    Task<List<ChatRoom>> GetUserRoomsAsync(string userId);

    /// <summary>
    /// 根据ID获取房间
    /// </summary>
    Task<ChatRoom?> GetRoomByIdAsync(string roomId);

    /// <summary>
    /// 验证房间密码
    /// </summary>
    Task<bool> VerifyPasswordAsync(string roomId, string password);

    /// <summary>
    /// 将用户添加到房间
    /// </summary>
    Task<bool> AddUserToRoomAsync(string userId, string roomId);

    /// <summary>
    /// 将用户从房间移除
    /// </summary>
    Task<bool> RemoveUserFromRoomAsync(string userId, string roomId);

    /// <summary>
    /// 获取房间成员数
    /// </summary>
    Task<int> GetRoomMemberCountAsync(string roomId);

    /// <summary>
    /// 检查用户是否在房间中
    /// </summary>
    Task<bool> IsUserInRoomAsync(string userId, string roomId);

    /// <summary>
    /// 根据房间名称查找房间（支持模糊搜索）
    /// </summary>
    Task<List<ChatRoom>> FindRoomsByNameAsync(string roomName);

    /// <summary>
    /// 根据房间名称获取房间（精确匹配，不区分大小写）
    /// </summary>
    Task<ChatRoom?> GetRoomByNameAsync(string roomName);

    /// <summary>
    /// 获取房间中的所有用户ID
    /// </summary>
    Task<List<string>> GetRoomUserIdsAsync(string roomId);
}
