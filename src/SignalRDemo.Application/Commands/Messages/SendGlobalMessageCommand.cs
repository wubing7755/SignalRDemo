using MediatR;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Results;

namespace SignalRDemo.Application.Commands.Messages;

/// <summary>
/// 发送全局消息Command
/// </summary>
public record SendGlobalMessageCommand(
    string UserId,
    string UserName,
    string Content,
    string? MediaUrl = null,
    string? AltText = null
) : IRequest<Result<ChatMessageDto>>;
