using MediatR;
using SignalRDemo.Application.DTOs;

namespace SignalRDemo.Application.Queries.Messages;

/// <summary>
/// 获取最近消息Query
/// </summary>
public record GetRecentMessagesQuery(int Count = 50) : IRequest<List<ChatMessageDto>>;
