using MediatR;
using SignalRDemo.Application.DTOs;

namespace SignalRDemo.Application.Queries.Messages;

/// <summary>
/// 获取房间消息历史查询
/// </summary>
public record GetRoomMessagesQuery(string RoomId, int Count = 50) : IRequest<List<ChatMessageDto>>;
