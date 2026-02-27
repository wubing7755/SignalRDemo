using MediatR;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Results;

namespace SignalRDemo.Application.Commands.Rooms;

/// <summary>
/// 按房间名称加入房间命令
/// </summary>
public record JoinRoomByNameCommand(
    string UserId,
    string RoomName,
    string? Password
) : IRequest<Result<RoomDto>>;
