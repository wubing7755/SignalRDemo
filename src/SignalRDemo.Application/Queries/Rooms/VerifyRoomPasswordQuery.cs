using MediatR;

namespace SignalRDemo.Application.Queries.Rooms;

/// <summary>
/// 验证房间密码查询
/// </summary>
public record VerifyRoomPasswordQuery(string RoomId, string Password) : IRequest<bool>;
