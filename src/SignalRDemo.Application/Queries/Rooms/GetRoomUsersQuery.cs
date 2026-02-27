using MediatR;

namespace SignalRDemo.Application.Queries.Rooms;

/// <summary>
/// 获取房间用户列表查询
/// </summary>
public record GetRoomUsersQuery(string RoomId) : IRequest<List<string>>;
