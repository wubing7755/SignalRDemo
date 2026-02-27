using MediatR;
using SignalRDemo.Application.DTOs;

namespace SignalRDemo.Application.Queries.Users;

/// <summary>
/// 根据ID获取用户Query
/// </summary>
public record GetUserByIdQuery(string UserId) : IRequest<UserDto?>;
