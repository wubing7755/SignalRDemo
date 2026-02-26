using MediatR;
using SignalRDemo.Application.Results;

namespace SignalRDemo.Application.Commands.Rooms;

public record LeaveRoomCommand(
    string UserId,
    string RoomId
) : IRequest<Result>;
