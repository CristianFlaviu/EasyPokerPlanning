using Microsoft.EntityFrameworkCore;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Infrastructure.Persistence;

public sealed class EmailLoginTokenRepository(PokerPlanningDbContext db) : IEmailLoginTokenRepository
{
    public Task<EmailLoginToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct) =>
        db.EmailLoginTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(EmailLoginToken token, CancellationToken ct) =>
        await db.EmailLoginTokens.AddAsync(token, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
