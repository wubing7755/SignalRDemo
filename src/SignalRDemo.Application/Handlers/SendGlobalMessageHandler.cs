using MediatR;
using SignalRDemo.Application.Commands.Messages;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Results;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;
using SignalRDemo.Shared.Models;
using DomainChatMessage = SignalRDemo.Domain.Entities.ChatMessage;

namespace SignalRDemo.Application.Handlers;

/// <summary>
/// 发送全局消息Handler
/// </summary>
public class SendGlobalMessageHandler : IRequestHandler<SendGlobalMessageCommand, Result<ChatMessageDto>>
{
    private readonly IMessageRepository _messageRepository;

    public SendGlobalMessageHandler(IMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public async Task<Result<ChatMessageDto>> Handle(SendGlobalMessageCommand request, CancellationToken cancellationToken)
    {
        // 创建消息实体 - 使用 Domain 的 ChatMessage
        var userId = UserId.Create(request.UserId);
        var userName = UserName.Create(request.UserName);
        var displayName = DisplayName.CreateOrDefault(request.UserName);
        var roomId = RoomId.Create("lobby"); // 全局消息使用 lobby 作为房间
        
        var message = DomainChatMessage.Create(
            userId,
            userName,
            displayName,
            roomId,
            request.Content,
            "Text",
            request.MediaUrl,
            request.AltText
        );

        // 保存消息
        await _messageRepository.AddAsync(message, cancellationToken);

        // 返回 DTO
        return Result<ChatMessageDto>.Success(new ChatMessageDto
        {
            Id = message.Id.Value,
            UserId = message.UserId.Value,
            UserName = message.UserName.Value,
            DisplayName = message.DisplayName?.Value,
            RoomId = message.RoomId.Value,
            Content = message.Content,
            Type = message.MessageType,
            MediaUrl = message.MediaUrl,
            AltText = message.AltText,
            Timestamp = message.Timestamp
        });
    }
}
