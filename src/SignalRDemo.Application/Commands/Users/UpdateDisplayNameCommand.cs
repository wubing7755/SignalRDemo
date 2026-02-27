using MediatR;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Results;

namespace SignalRDemo.Application.Commands.Users;

/// <summary>
/// 更新显示昵称Command
/// </summary>
public record UpdateDisplayNameCommand(
    string UserId,
    string DisplayName
) : IRequest<Result<UserDto>>;
