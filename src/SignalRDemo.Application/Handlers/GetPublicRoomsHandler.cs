using MediatR;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Queries.Rooms;
using SignalRDemo.Domain.Repositories;

namespace SignalRDemo.Application.Handlers;

/// <summary>
/// 获取公共房间列表处理器
/// </summary>
public class GetPublicRoomsHandler : IRequestHandler<GetPublicRoomsQuery, List<RoomDto>>
{
    private readonly IRoomRepository _roomRepository;

    public GetPublicRoomsHandler(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<List<RoomDto>> Handle(GetPublicRoomsQuery request, CancellationToken cancellationToken)
    {
        var rooms = await _roomRepository.GetPublicRoomsAsync(cancellationToken);
        
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
