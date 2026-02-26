using MediatR;
using SignalRDemo.Application.Commands.Users;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Results;
using SignalRDemo.Domain.Aggregates;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Application.Handlers;

public class LoginHandler : IRequestHandler<LoginCommand, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public LoginHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userName = UserName.Create(request.UserName);
            var password = Password.Create(request.Password);

            var user = await _userRepository.GetByUserNameAsync(userName, cancellationToken);
            if (user == null)
            {
                return Result<UserDto>.Failure("用户名或密码错误", "INVALID_CREDENTIALS");
            }

            if (!user.VerifyPassword(password))
            {
                return Result<UserDto>.Failure("用户名或密码错误", "INVALID_CREDENTIALS");
            }

            user.Login();
            await _userRepository.UpdateAsync(user, cancellationToken);

            return Result<UserDto>.Success(MapToDto(user));
        }
        catch (ArgumentException ex)
        {
            return Result<UserDto>.Failure(ex.Message, "VALIDATION_ERROR");
        }
        catch (Exception ex)
        {
            return Result<UserDto>.Failure("登录失败: " + ex.Message, "LOGIN_ERROR");
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
