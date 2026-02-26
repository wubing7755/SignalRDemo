using SignalRDemo.Domain.Events;
using SignalRDemo.Domain.Exceptions;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Domain.Aggregates;

public class User : AggregateRoot<UserId>
{
    public UserName UserName { get; private set; } = null!;
    public DisplayName DisplayName { get; private set; } = null!;
    public HashedPassword PasswordHash { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // 私有构造函数 - 通过工厂方法创建
    private User() { }

    public static User Create(UserName userName, Password password, DisplayName? displayName)
    {
        var user = new User
        {
            Id = UserId.New(),
            UserName = userName,
            DisplayName = displayName ?? DisplayName.FromUserName(userName),
            PasswordHash = password.Hash(),
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };

        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.UserName, user.DisplayName));

        return user;
    }

    public static User Reconstitute(
        UserId id,
        UserName userName,
        DisplayName displayName,
        HashedPassword passwordHash,
        DateTime createdAt,
        DateTime? lastLoginAt,
        int version = 1)
    {
        return new User
        {
            Id = id,
            UserName = userName,
            DisplayName = displayName,
            PasswordHash = passwordHash,
            CreatedAt = createdAt,
            LastLoginAt = lastLoginAt,
            Version = version
        };
    }

    public bool VerifyPassword(Password password)
    {
        return PasswordHash.Verify(password);
    }

    public void Login()
    {
        LastLoginAt = DateTime.UtcNow;
        AddDomainEvent(new UserLoggedInEvent(Id, UserName));
    }

    public void ChangeDisplayName(DisplayName newDisplayName)
    {
        var oldDisplayName = DisplayName;
        DisplayName = newDisplayName;
        AddDomainEvent(new DisplayNameChangedEvent(Id, oldDisplayName, newDisplayName));
    }

    public override void Apply(DomainEvent @event)
    {
        switch (@event)
        {
            case UserRegisteredEvent e:
                Id = e.UserId;
                UserName = e.UserName;
                DisplayName = e.DisplayName;
                CreatedAt = e.OccurredAt;
                break;

            case UserLoggedInEvent e:
                LastLoginAt = e.OccurredAt;
                break;

            case DisplayNameChangedEvent e:
                DisplayName = e.NewDisplayName;
                break;
        }
    }
}
