using MediatR;
using SignalRDemo.Application.DTOs;

namespace SignalRDemo.Application.Queries.Rooms;

/// <summary>
/// 获取用户房间列表查询
/// </summary>
public record GetUserRoomsQuery(string UserId) : IRequest<List<RoomDto>>;
