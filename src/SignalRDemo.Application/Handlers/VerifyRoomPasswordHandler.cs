using MediatR;
using SignalRDemo.Application.Queries.Rooms;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Application.Handlers;

/// <summary>
/// 验证房间密码处理器
/// </summary>
public class VerifyRoomPasswordHandler : IRequestHandler<VerifyRoomPasswordQuery, bool>
{
    private readonly IRoomRepository _roomRepository;

    public VerifyRoomPasswordHandler(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<bool> Handle(VerifyRoomPasswordQuery request, CancellationToken cancellationToken)
    {
        var roomId = RoomId.Create(request.RoomId);
        var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
        
        if (room == null || room.IsPublic)
        {
            return false;
        }

        try
        {
            var password = Password.Create(request.Password);
            return room.VerifyPassword(password);
        }
        catch
        {
            return false;
        }
    }
}
