using MediatR;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Queries.Users;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Application.Handlers;

/// <summary>
/// 根据ID获取用户Handler
/// </summary>
public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = UserId.Create(request.UserId);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user == null)
        {
            return null;
        }

        return new UserDto
        {
            Id = user.Id.Value,
            UserName = user.UserName.Value,
            DisplayName = user.DisplayName?.Value
        };
    }
}
