using MediatR;
using SignalRDemo.Application.Commands.Users;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Results;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Application.Handlers;

/// <summary>
/// 更新显示昵称Handler
/// </summary>
public class UpdateDisplayNameHandler : IRequestHandler<UpdateDisplayNameCommand, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public UpdateDisplayNameHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> Handle(UpdateDisplayNameCommand request, CancellationToken cancellationToken)
    {
        var userId = UserId.Create(request.UserId);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user == null)
        {
            return Result<UserDto>.Failure("用户不存在");
        }

        // 更新显示昵称
        user.ChangeDisplayName(DisplayName.Create(request.DisplayName));
        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<UserDto>.Success(new UserDto
        {
            Id = user.Id.Value,
            UserName = user.UserName.Value,
            DisplayName = user.DisplayName?.Value
        });
    }
}
