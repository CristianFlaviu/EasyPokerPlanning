using PokerPlanning.Application.Abstractions.Security;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Infrastructure.Security;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public PasswordHash Hash(string plaintext) =>
        new(BCrypt.Net.BCrypt.HashPassword(plaintext, WorkFactor));

    public bool Verify(string plaintext, PasswordHash hash) =>
        BCrypt.Net.BCrypt.Verify(plaintext, hash.Value);
}
