using MediatR;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Queries.Messages;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Shared.Models;

namespace SignalRDemo.Application.Handlers;

/// <summary>
/// 获取最近消息Handler
/// </summary>
public class GetRecentMessagesHandler : IRequestHandler<GetRecentMessagesQuery, List<ChatMessageDto>>
{
    private readonly IMessageRepository _messageRepository;

    public GetRecentMessagesHandler(IMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public async Task<List<ChatMessageDto>> Handle(GetRecentMessagesQuery request, CancellationToken cancellationToken)
    {
        var messages = await _messageRepository.GetRecentMessagesAsync(request.Count, cancellationToken);

        return messages.Select(m => new ChatMessageDto
        {
            Id = m.Id.Value,
            UserId = m.UserId.Value,
            UserName = m.UserName.Value,
            DisplayName = m.DisplayName?.Value,
            RoomId = m.RoomId.Value,
            Content = m.Content,
            Type = m.MessageType,
            MediaUrl = m.MediaUrl,
            AltText = m.AltText,
            Timestamp = m.Timestamp
        }).ToList();
    }
}
