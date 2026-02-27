using MediatR;
using SignalRDemo.Application.DTOs;

namespace SignalRDemo.Application.Queries.Rooms;

/// <summary>
/// 搜索房间查询
/// </summary>
public record SearchRoomsQuery(string SearchTerm) : IRequest<List<RoomDto>>;
