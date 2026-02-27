using MediatR;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Queries.Rooms;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Application.Handlers;

/// <summary>
/// 获取用户房间列表处理器
/// </summary>
public class GetUserRoomsHandler : IRequestHandler<GetUserRoomsQuery, List<RoomDto>>
{
    private readonly IRoomRepository _roomRepository;

    public GetUserRoomsHandler(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<List<RoomDto>> Handle(GetUserRoomsQuery request, CancellationToken cancellationToken)
    {
        var userId = UserId.Create(request.UserId);
        var rooms = await _roomRepository.GetUserRoomsAsync(userId, cancellationToken);
        
        return rooms.Select(room => new RoomDto
        {
            Id = room.Id.Value,
            Name = room.Name.Value,
            Description = room.Description,
            OwnerId = room.OwnerId.Value,
            IsPublic = room.IsPublic,
            CreatedAt = room.CreatedAt,
            MemberCount = room.MemberCount
        }).ToList();
    }
}
