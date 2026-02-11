using SignalRDemo.Shared.Models;

namespace SignalRDemo.Client.Services.Abstractions;

/// <summary>
/// 房间服务接口 - 管理房间业务逻辑
/// </summary>
public interface IRoomService
{
    /// <summary>
    /// 当前房间
    /// </summary>
    ChatRoom? CurrentRoom { get; }
    
    /// <summary>
    /// 当前房间ID
    /// </summary>
    string? CurrentRoomId { get; }
    
    /// <summary>
    /// 所有公共房间
    /// </summary>
    IReadOnlyList<ChatRoom> PublicRooms { get; }
    
    /// <summary>
    /// 我的房间列表
    /// </summary>
    IReadOnlyList<ChatRoom> MyRooms { get; }
    
    /// <summary>
    /// 当前房间变化事件
    /// </summary>
    event Action<ChatRoom?>? CurrentRoomChanged;
    
    /// <summary>
    /// 房间列表更新事件
    /// </summary>
    event Action<IReadOnlyList<ChatRoom>>? PublicRoomsUpdated;
    
    /// <summary>
    /// 我的房间列表更新事件
    /// </summary>
    event Action<IReadOnlyList<ChatRoom>>? MyRoomsUpdated;
    
    /// <summary>
    /// 房间创建完成事件
    /// </summary>
    event Action<JoinRoomResponse>? RoomCreated;
    
    /// <summary>
    /// 加入房间完成事件
    /// </summary>
    event Action<JoinRoomResponse>? RoomJoined;
    
    /// <summary>
    /// 离开房间完成事件
    /// </summary>
    event Action<string>? RoomLeft;
    
    /// <summary>
    /// 初始化服务
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// 获取公共房间列表
    /// </summary>
    Task RefreshPublicRoomsAsync();
    
    /// <summary>
    /// 获取我的房间列表
    /// </summary>
    Task RefreshMyRoomsAsync();
    
    /// <summary>
    /// 创建房间
    /// </summary>
    Task CreateRoomAsync(string name, string? description, bool isPublic, string? password);
    
    /// <summary>
    /// 加入房间
    /// </summary>
    Task<JoinRoomResponse> JoinRoomAsync(string roomId, string? password = null);
    
    /// <summary>
    /// 离开当前房间
    /// </summary>
    Task LeaveRoomAsync();
    
    /// <summary>
    /// 切换到指定房间
    /// </summary>
    Task SwitchRoomAsync(string roomId, string? password = null);
    
    /// <summary>
    /// 验证房间密码
    /// </summary>
    Task<bool> VerifyRoomPasswordAsync(string roomId, string password);
}
