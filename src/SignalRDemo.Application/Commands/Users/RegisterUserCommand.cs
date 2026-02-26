using MediatR;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Results;

namespace SignalRDemo.Application.Commands.Users;

public record RegisterUserCommand(
    string UserName,
    string Password,
    string? DisplayName
) : IRequest<Result<UserDto>>;
