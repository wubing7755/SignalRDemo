using SignalRDemo.Client.Services.Abstractions;
using SignalRDemo.Shared.Models;

namespace SignalRDemo.Client.Services;

/// <summary>
/// 房间服务实现 - 管理房间业务逻辑
/// </summary>
public class RoomService : IRoomService
{
    private readonly ISignalRConnectionService _connectionService;
    private readonly IUserStateService _userStateService;
    private readonly ILogger<RoomService> _logger;

    private ChatRoom? _currentRoom;
    private readonly List<ChatRoom> _publicRooms = new();
    private readonly List<ChatRoom> _myRooms = new();

    public ChatRoom? CurrentRoom => _currentRoom;
    public string? CurrentRoomId => _currentRoom?.Id;
    public IReadOnlyList<ChatRoom> PublicRooms => _publicRooms.AsReadOnly();
    public IReadOnlyList<ChatRoom> MyRooms => _myRooms.AsReadOnly();

    public event Action<ChatRoom?>? CurrentRoomChanged;
    public event Action<IReadOnlyList<ChatRoom>>? PublicRoomsUpdated;
    public event Action<IReadOnlyList<ChatRoom>>? MyRoomsUpdated;
    public event Action<JoinRoomResponse>? RoomCreated;
    public event Action<JoinRoomResponse>? RoomJoined;
    public event Action<string>? RoomLeft;

    public RoomService(
        ISignalRConnectionService connectionService,
        IUserStateService userStateService,
        ILogger<RoomService> logger)
    {
        _connectionService = connectionService;
        _userStateService = userStateService;
        _logger = logger;
    }

    public Task InitializeAsync()
    {
        // 注册 SignalR 事件处理
        _connectionService.On<JoinRoomResponse>("RoomCreated", OnRoomCreated);
        _connectionService.On<JoinRoomResponse>("JoinRoomResult", OnJoinRoomResult);

        return Task.CompletedTask;
    }

    public async Task RefreshPublicRoomsAsync()
    {
        try
        {
            var rooms = await _connectionService.InvokeAsync<List<ChatRoom>>("GetRooms");
            if (rooms != null)
            {
                _publicRooms.Clear();
                _publicRooms.AddRange(rooms);
                PublicRoomsUpdated?.Invoke(_publicRooms.AsReadOnly());
                _logger.LogInformation("刷新公共房间列表: {Count} 个房间", rooms.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新公共房间列表失败");
        }
    }

    public async Task RefreshMyRoomsAsync()
    {
        if (!_userStateService.IsLoggedIn) return;

        try
        {
            var rooms = await _connectionService.InvokeAsync<List<ChatRoom>>("GetMyRooms");
            if (rooms != null)
            {
                _myRooms.Clear();
                _myRooms.AddRange(rooms);
                MyRoomsUpdated?.Invoke(_myRooms.AsReadOnly());
                _logger.LogInformation("刷新我的房间列表: {Count} 个房间", rooms.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新我的房间列表失败");
        }
    }

    public async Task CreateRoomAsync(string name, string? description, bool isPublic, string? password)
    {
        if (!_userStateService.IsLoggedIn)
        {
            RoomCreated?.Invoke(new JoinRoomResponse
            {
                Success = false,
                Message = "请先登录"
            });
            return;
        }

        var request = new CreateRoomRequest
        {
            Name = name,
            Description = description,
            IsPublic = isPublic,
            Password = isPublic ? null : password
        };

        try
        {
            await _connectionService.InvokeAsync("CreateRoom", request);
            // 结果通过 RoomCreated 事件返回
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建房间失败");
            RoomCreated?.Invoke(new JoinRoomResponse
            {
                Success = false,
                Message = "创建房间失败，请稍后重试"
            });
        }
    }

    public async Task<JoinRoomResponse> JoinRoomAsync(string roomId, string? password = null)
    {
        if (!_userStateService.IsLoggedIn)
        {
            return new JoinRoomResponse
            {
                Success = false,
                Message = "请先登录"
            };
        }

        var request = new JoinRoomRequest
        {
            RoomId = roomId,
            Password = password
        };

        try
        {
            var response = await _connectionService.InvokeAsync<JoinRoomResponse>("JoinRoom", request);
            if (response?.Success == true)
            {
                _currentRoom = response.Room;
                CurrentRoomChanged?.Invoke(_currentRoom);
                _logger.LogInformation("加入房间成功: {RoomName}", response.Room?.Name);
            }
            return response ?? new JoinRoomResponse
            {
                Success = false,
                Message = "加入房间失败"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加入房间失败");
            return new JoinRoomResponse
            {
                Success = false,
                Message = "加入房间失败，请稍后重试"
            };
        }
    }

    public async Task LeaveRoomAsync()
    {
        if (_currentRoom == null) return;

        var roomId = _currentRoom.Id;

        try
        {
            await _connectionService.InvokeAsync("LeaveRoom", roomId);
            _currentRoom = null;
            CurrentRoomChanged?.Invoke(null);
            RoomLeft?.Invoke(roomId);
            _logger.LogInformation("离开房间: {RoomId}", roomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "离开房间失败");
        }
    }

    public async Task SwitchRoomAsync(string roomId, string? password = null)
    {
        // 1. 先离开当前房间
        if (_currentRoom != null && _currentRoom.Id != roomId)
        {
            try
            {
                await _connectionService.InvokeAsync("LeaveRoom", _currentRoom.Id);
                RoomLeft?.Invoke(_currentRoom.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "离开旧房间时发生错误");
            }
        }

        // 2. 加入新房间
        var response = await JoinRoomAsync(roomId, password);
        if (!response.Success)
        {
            _logger.LogWarning("切换房间失败: {Message}", response.Message);
        }
    }

    public async Task<bool> VerifyRoomPasswordAsync(string roomId, string password)
    {
        try
        {
            return await _connectionService.InvokeAsync<bool>("VerifyRoomPassword", roomId, password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证房间密码失败");
            return false;
        }
    }

    private void OnRoomCreated(JoinRoomResponse response)
    {
        if (response.Success && response.Room != null)
        {
            _currentRoom = response.Room;
            CurrentRoomChanged?.Invoke(_currentRoom);
            _myRooms.Add(response.Room);
            MyRoomsUpdated?.Invoke(_myRooms.AsReadOnly());
        }
        RoomCreated?.Invoke(response);
    }

    private void OnJoinRoomResult(JoinRoomResponse response)
    {
        RoomJoined?.Invoke(response);
    }
}
