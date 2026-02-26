using MediatR;
using SignalRDemo.Application.Commands.Messages;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Results;
using SignalRDemo.Domain.Entities;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Application.Handlers;

public class SendMessageHandler : IRequestHandler<SendMessageCommand, Result<ChatMessageDto>>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IRoomRepository _roomRepository;

    public SendMessageHandler(IMessageRepository messageRepository, IRoomRepository roomRepository)
    {
        _messageRepository = messageRepository;
        _roomRepository = roomRepository;
    }

    public async Task<Result<ChatMessageDto>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = UserId.Create(request.UserId);
            var roomId = RoomId.Create(request.RoomId);
            var userName = UserName.Create(request.UserName);
            var displayName = DisplayName.CreateOrDefault(request.UserName) ?? DisplayName.FromUserName(userName);

            // 检查房间是否存在
            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
            {
                return Result<ChatMessageDto>.Failure("房间不存在", "ROOM_NOT_FOUND");
            }

            // 检查用户是否在房间中
            if (!room.ContainsMember(userId))
            {
                return Result<ChatMessageDto>.Failure("您不在该房间中", "NOT_IN_ROOM");
            }

            // 创建消息
            var message = ChatMessage.Create(
                userId,
                userName,
                displayName,
                roomId,
                request.Content,
                request.MessageType,
                request.MediaUrl,
                request.AltText);

            await _messageRepository.AddAsync(message, cancellationToken);

            return Result<ChatMessageDto>.Success(MapToDto(message));
        }
        catch (Exception ex)
        {
            return Result<ChatMessageDto>.Failure("发送消息失败: " + ex.Message, "SEND_MESSAGE_ERROR");
        }
    }

    private static ChatMessageDto MapToDto(ChatMessage message) => new()
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
    };
}
