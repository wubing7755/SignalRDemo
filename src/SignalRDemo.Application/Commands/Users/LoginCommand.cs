using MediatR;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Results;

namespace SignalRDemo.Application.Commands.Users;

public record LoginCommand(
    string UserName,
    string Password
) : IRequest<Result<UserDto>>;
