using MediatR;
using SignalRDemo.Application.Commands.Rooms;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Results;
using SignalRDemo.Domain.Aggregates;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Application.Handlers;

public class JoinRoomHandler : IRequestHandler<JoinRoomCommand, Result<RoomDto>>
{
    private readonly IRoomRepository _roomRepository;

    public JoinRoomHandler(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<Result<RoomDto>> Handle(JoinRoomCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var roomId = RoomId.Create(request.RoomId);
            var userId = UserId.Create(request.UserId);

            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
            {
                return Result<RoomDto>.Failure("房间不存在", "ROOM_NOT_FOUND");
            }

            // 验证密码（如果是私人房间）
            if (!room.IsPublic)
            {
                if (string.IsNullOrEmpty(request.Password))
                {
                    return Result<RoomDto>.Failure("该房间需要密码", "PASSWORD_REQUIRED");
                }

                var password = Password.Create(request.Password);
                if (!room.VerifyPassword(password))
                {
                    return Result<RoomDto>.Failure("房间密码错误", "INVALID_PASSWORD");
                }
            }

            // 检查用户是否已在房间中
            if (room.ContainsMember(userId))
            {
                return Result<RoomDto>.Failure("您已在房间中", "ALREADY_IN_ROOM");
            }

            // 添加用户到房间
            room.AddMember(userId);
            await _roomRepository.UpdateAsync(room, cancellationToken);

            return Result<RoomDto>.Success(MapToDto(room));
        }
        catch (ArgumentException ex)
        {
            return Result<RoomDto>.Failure(ex.Message, "VALIDATION_ERROR");
        }
        catch (Exception ex)
        {
            return Result<RoomDto>.Failure("加入房间失败: " + ex.Message, "JOIN_ROOM_ERROR");
        }
    }

    private static RoomDto MapToDto(ChatRoom room) => new()
    {
        Id = room.Id.Value,
        Name = room.Name.Value,
        Description = room.Description,
        OwnerId = room.OwnerId.Value,
        IsPublic = room.IsPublic,
        CreatedAt = room.CreatedAt,
        MemberCount = room.MemberCount
    };
}
