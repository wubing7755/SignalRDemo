using SignalRDemo.Domain.Aggregates;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Infrastructure.Repositories;

public class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var user = _users.FirstOrDefault(u => u.Id.Value == id.Value);
            return Task.FromResult(user);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<User?> GetByUserNameAsync(UserName userName, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var user = _users.FirstOrDefault(u => 
                u.UserName.Value.Equals(userName.Value, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(user);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<bool> ExistsAsync(UserName userName, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var exists = _users.Any(u => 
                u.UserName.Value.Equals(userName.Value, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            _users.Add(user);
            return Task.FromResult(user);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var index = _users.FindIndex(u => u.Id.Value == user.Id.Value);
            if (index >= 0)
            {
                _users[index] = user;
            }
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task DeleteAsync(UserId id, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            _users.RemoveAll(u => u.Id.Value == id.Value);
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
