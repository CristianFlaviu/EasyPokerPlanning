using PokerPlanning.Domain.Users;

namespace PokerPlanning.Application.Abstractions.Persistence;

public interface IEmailLoginTokenRepository
{
    Task<EmailLoginToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct);
    Task AddAsync(EmailLoginToken token, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
