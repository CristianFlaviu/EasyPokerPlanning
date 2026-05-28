using Microsoft.EntityFrameworkCore;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Infrastructure.Persistence;

public sealed class UserRepository(PokerPlanningDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(UserId id, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByExternalLoginAsync(string provider, string subject, CancellationToken ct)
    {
        var users = await db.Users.ToListAsync(ct);
        return users.FirstOrDefault(u =>
            u.Logins.Any(l => l.Provider == provider && l.Subject == subject));
    }

    public async Task AddAsync(User user, CancellationToken ct) =>
        await db.Users.AddAsync(user, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
