using MediatR;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Results;

namespace SignalRDemo.Application.Commands.Rooms;

public record CreateRoomCommand(
    string Name,
    string? Description,
    string OwnerId,
    bool IsPublic,
    string? Password
) : IRequest<Result<RoomDto>>;
