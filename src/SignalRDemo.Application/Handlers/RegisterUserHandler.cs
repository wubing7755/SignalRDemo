using MediatR;
using SignalRDemo.Application.Commands.Users;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Results;
using SignalRDemo.Domain.Aggregates;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Application.Handlers;

public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public RegisterUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userName = UserName.Create(request.UserName);
            var password = Password.Create(request.Password);
            var displayName = DisplayName.CreateOrDefault(request.DisplayName);

            if (await _userRepository.ExistsAsync(userName, cancellationToken))
            {
                return Result<UserDto>.Failure("用户名已存在", "USER_ALREADY_EXISTS");
            }

            var user = User.Create(userName, password, displayName);
            await _userRepository.AddAsync(user, cancellationToken);

            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (ArgumentException ex)
        {
            return Result<UserDto>.Failure(ex.Message, "VALIDATION_ERROR");
        }
        catch (Exception ex)
        {
            return Result<UserDto>.Failure("注册失败: " + ex.Message, "REGISTER_ERROR");
        }
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id.Value,
        UserName = user.UserName.Value,
        DisplayName = user.DisplayName.Value,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt
    };
}
