using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Domain.Events;

public abstract record DomainEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public int Version { get; set; } = 1;
}

// ==================== User Events ====================

public record UserRegisteredEvent(
    UserId UserId,
    UserName UserName,
    DisplayName DisplayName
) : DomainEvent;

public record UserLoggedInEvent(
    UserId UserId,
    UserName UserName
) : DomainEvent;

public record DisplayNameChangedEvent(
    UserId UserId,
    DisplayName OldDisplayName,
    DisplayName NewDisplayName
) : DomainEvent;

// ==================== Room Events ====================

public record RoomCreatedEvent(
    RoomId RoomId,
    RoomName Name,
    UserId OwnerId,
    bool IsPublic
) : DomainEvent;

public record UserJoinedRoomEvent(
    UserId UserId,
    RoomId RoomId
) : DomainEvent;

public record UserLeftRoomEvent(
    UserId UserId,
    RoomId RoomId
) : DomainEvent;

// ==================== Message Events ====================

public record MessageSentEvent(
    Guid MessageId,
    UserId UserId,
    UserName UserName,
    RoomId RoomId,
    string Content,
    string MessageType
) : DomainEvent;
