using SignalRDemo.Domain.Events;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Domain.Aggregates;

public class ChatRoom : AggregateRoot<RoomId>
{
    public RoomName Name { get; set; } = null!;
    public string? Description { get; set; }
    public UserId OwnerId { get; set; } = null!;
    public bool IsPublic { get; set; }
    public HashedPassword? Password { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }

    private readonly List<UserId> _memberIds = new();
    public IReadOnlyList<UserId> MemberIds => _memberIds.AsReadOnly();

    // 私有构造函数 - 通过工厂方法创建
    private ChatRoom() { }

    public static ChatRoom Create(
        RoomName name,
        string? description,
        UserId ownerId,
        bool isPublic,
        Password? password)
    {
        var room = new ChatRoom
        {
            Id = RoomId.New(),
            Name = name,
            Description = description,
            OwnerId = ownerId,
            IsPublic = isPublic,
            Password = isPublic ? null : password?.Hash(),
            CreatedAt = DateTime.UtcNow,
            MemberCount = 1,
            Version = 1
        };

        // 创建者自动加入房间
        room._memberIds.Add(ownerId);
        room.AddDomainEvent(new RoomCreatedEvent(room.Id, room.Name, room.OwnerId, room.IsPublic));
        room.AddDomainEvent(new UserJoinedRoomEvent(ownerId, room.Id));

        return room;
    }

    public static ChatRoom CreateLobby()
    {
        var room = new ChatRoom
        {
            Id = RoomId.From("lobby"),
            Name = RoomName.Create("大厅"),
            Description = "公共聊天大厅，欢迎所有人加入",
            OwnerId = UserId.From("system"),
            IsPublic = true,
            CreatedAt = DateTime.UtcNow,
            MemberCount = 0,
            Version = 1
        };

        return room;
    }

    public static ChatRoom Reconstitute(
        RoomId id,
        RoomName name,
        string? description,
        UserId ownerId,
        bool isPublic,
        HashedPassword? password,
        DateTime createdAt,
        int memberCount,
        List<UserId>? memberIds,
        int version = 1)
    {
        var room = new ChatRoom
        {
            Id = id,
            Name = name,
            Description = description,
            OwnerId = ownerId,
            IsPublic = isPublic,
            Password = password,
            CreatedAt = createdAt,
            MemberCount = memberCount,
            Version = version
        };
        
        if (memberIds != null)
        {
            room._memberIds.AddRange(memberIds);
        }
        
        return room;
    }

    public bool VerifyPassword(Password password)
    {
        if (IsPublic || Password == null)
            return true;
        return Password.Verify(password);
    }

    public bool ContainsMember(UserId userId)
    {
        return _memberIds.Contains(userId);
    }

    public void AddMember(UserId userId)
    {
        if (!_memberIds.Contains(userId))
        {
            _memberIds.Add(userId);
            MemberCount++;
            AddDomainEvent(new UserJoinedRoomEvent(userId, Id));
        }
    }

    public void RemoveMember(UserId userId)
    {
        if (_memberIds.Remove(userId) && MemberCount > 0)
        {
            MemberCount--;
            AddDomainEvent(new UserLeftRoomEvent(userId, Id));
        }
    }

    public override void Apply(DomainEvent @event)
    {
        switch (@event)
        {
            case RoomCreatedEvent e:
                // 只在构造函数外部应用事件时设置属性
                break;

            case UserJoinedRoomEvent e:
                if (!_memberIds.Contains(e.UserId))
                {
                    _memberIds.Add(e.UserId);
                    MemberCount++;
                }
                break;

            case UserLeftRoomEvent e:
                _memberIds.Remove(e.UserId);
                if (MemberCount > 0) MemberCount--;
                break;
        }
    }
}
