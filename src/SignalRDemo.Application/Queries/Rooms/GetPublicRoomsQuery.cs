using MediatR;
using SignalRDemo.Application.DTOs;

namespace SignalRDemo.Application.Queries.Rooms;

/// <summary>
/// 获取公共房间列表查询
/// </summary>
public record GetPublicRoomsQuery : IRequest<List<RoomDto>>;
