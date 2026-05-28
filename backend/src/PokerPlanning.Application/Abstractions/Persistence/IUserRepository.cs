using PokerPlanning.Domain.Users;

namespace PokerPlanning.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken ct);
    Task<User?> GetByExternalLoginAsync(string provider, string subject, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
