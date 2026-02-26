using MediatR;
using SignalRDemo.Application.Commands.Rooms;
using SignalRDemo.Application.Results;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Application.Handlers;

public class LeaveRoomHandler : IRequestHandler<LeaveRoomCommand, Result>
{
    private readonly IRoomRepository _roomRepository;

    public LeaveRoomHandler(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<Result> Handle(LeaveRoomCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var roomId = RoomId.Create(request.RoomId);
            var userId = UserId.Create(request.UserId);

            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
            {
                return Result.Failure("房间不存在", "ROOM_NOT_FOUND");
            }

            // 检查用户是否在房间中
            if (!room.ContainsMember(userId))
            {
                return Result.Failure("您不在该房间中", "NOT_IN_ROOM");
            }

            // 从房间移除用户
            room.RemoveMember(userId);
            await _roomRepository.UpdateAsync(room, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure("离开房间失败: " + ex.Message, "LEAVE_ROOM_ERROR");
        }
    }
}
