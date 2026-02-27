using MediatR;
using SignalRDemo.Application.Queries.Rooms;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Application.Handlers;

/// <summary>
/// 检查用户是否在房间中处理器
/// </summary>
public class IsUserInRoomHandler : IRequestHandler<IsUserInRoomQuery, bool>
{
    private readonly IRoomRepository _roomRepository;

    public IsUserInRoomHandler(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<bool> Handle(IsUserInRoomQuery request, CancellationToken cancellationToken)
    {
        var roomId = RoomId.Create(request.RoomId);
        var userId = UserId.Create(request.UserId);
        
        var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
        
        if (room == null)
        {
            return false;
        }

        return room.ContainsMember(userId);
    }
}
