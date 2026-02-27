using MediatR;
using SignalRDemo.Application.Queries.Rooms;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Application.Handlers;

/// <summary>
/// 获取房间用户列表处理器
/// </summary>
public class GetRoomUsersHandler : IRequestHandler<GetRoomUsersQuery, List<string>>
{
    private readonly IRoomRepository _roomRepository;
    private readonly IUserRepository _userRepository;

    public GetRoomUsersHandler(IRoomRepository roomRepository, IUserRepository userRepository)
    {
        _roomRepository = roomRepository;
        _userRepository = userRepository;
    }

    public async Task<List<string>> Handle(GetRoomUsersQuery request, CancellationToken cancellationToken)
    {
        var roomId = RoomId.Create(request.RoomId);
        var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
        
        if (room == null)
        {
            return new List<string>();
        }

            var userNames = new List<string>();
            foreach (var userId in room.MemberIds)
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user != null)
                {
                    userNames.Add(user.DisplayName.Value);
                }
            }

        return userNames;
    }
}
