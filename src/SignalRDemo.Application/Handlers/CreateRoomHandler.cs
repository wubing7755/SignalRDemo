using MediatR;
using SignalRDemo.Application.Commands.Rooms;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Results;
using SignalRDemo.Domain.Aggregates;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Application.Handlers;

public class CreateRoomHandler : IRequestHandler<CreateRoomCommand, Result<RoomDto>>
{
    private readonly IRoomRepository _roomRepository;

    public CreateRoomHandler(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<Result<RoomDto>> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var roomName = RoomName.Create(request.Name);
            var ownerId = UserId.Create(request.OwnerId);
            Password? password = null;

            if (!request.IsPublic && !string.IsNullOrEmpty(request.Password))
            {
                password = Password.Create(request.Password);
            }

            var existingRoom = await _roomRepository.GetByNameAsync(roomName, cancellationToken);
            if (existingRoom != null)
            {
                return Result<RoomDto>.Failure("房间名称已存在", "ROOM_ALREADY_EXISTS");
            }

            var room = ChatRoom.Create(roomName, request.Description, ownerId, request.IsPublic, password);
            await _roomRepository.AddAsync(room, cancellationToken);

            return Result<RoomDto>.Success(MapToDto(room));
        }
        catch (ArgumentException ex)
        {
            return Result<RoomDto>.Failure(ex.Message, "VALIDATION_ERROR");
        }
        catch (Exception ex)
        {
            return Result<RoomDto>.Failure("创建房间失败: " + ex.Message, "CREATE_ROOM_ERROR");
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
