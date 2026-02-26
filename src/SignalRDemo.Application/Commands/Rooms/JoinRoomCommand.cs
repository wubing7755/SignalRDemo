using MediatR;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Results;

namespace SignalRDemo.Application.Commands.Rooms;

public record JoinRoomCommand(
    string UserId,
    string RoomId,
    string? Password
) : IRequest<Result<RoomDto>>;
