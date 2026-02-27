using MediatR;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Queries.Messages;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Application.Handlers;

/// <summary>
/// 获取房间消息历史处理器
/// </summary>
public class GetRoomMessagesHandler : IRequestHandler<GetRoomMessagesQuery, List<ChatMessageDto>>
{
    private readonly IMessageRepository _messageRepository;

    public GetRoomMessagesHandler(IMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public async Task<List<ChatMessageDto>> Handle(GetRoomMessagesQuery request, CancellationToken cancellationToken)
    {
        var roomId = RoomId.Create(request.RoomId);
        var messages = await _messageRepository.GetRoomMessagesAsync(roomId, request.Count, cancellationToken);
        
        return messages.Select(message => new ChatMessageDto
        {
            Id = message.Id.Value,
            UserId = message.UserId.Value,
            UserName = message.UserName.Value,
            DisplayName = message.DisplayName.Value,
            RoomId = message.RoomId.Value,
            Content = message.Content,
            Type = message.MessageType,
            MediaUrl = message.MediaUrl,
            AltText = message.AltText,
            Timestamp = message.Timestamp
        }).ToList();
    }
}
