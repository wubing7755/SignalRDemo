using SignalRDemo.Domain.Aggregates;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);
    Task<User?> GetByUserNameAsync(UserName userName, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(UserName userName, CancellationToken cancellationToken = default);
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserId id, CancellationToken cancellationToken = default);
}
