using MediatR;

namespace SignalRDemo.Application.Queries.Rooms;

/// <summary>
/// 检查用户是否在房间中查询
/// </summary>
public record IsUserInRoomQuery(string UserId, string RoomId) : IRequest<bool>;
