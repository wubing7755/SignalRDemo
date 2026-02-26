using MediatR;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Results;

namespace SignalRDemo.Application.Commands.Messages;

public record SendMessageCommand(
    string UserId,
    string UserName,
    string RoomId,
    string Content,
    string MessageType = "Text",
    string? MediaUrl = null,
    string? AltText = null
) : IRequest<Result<ChatMessageDto>>;
