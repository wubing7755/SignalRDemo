using MediatR;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Queries.Rooms;
using SignalRDemo.Domain.Repositories;

namespace SignalRDemo.Application.Handlers;

/// <summary>
/// 搜索房间处理器
/// </summary>
public class SearchRoomsHandler : IRequestHandler<SearchRoomsQuery, List<RoomDto>>
{
    private readonly IRoomRepository _roomRepository;

    public SearchRoomsHandler(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<List<RoomDto>> Handle(SearchRoomsQuery request, CancellationToken cancellationToken)
    {
        var rooms = await _roomRepository.SearchByNameAsync(request.SearchTerm, cancellationToken);
        
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
