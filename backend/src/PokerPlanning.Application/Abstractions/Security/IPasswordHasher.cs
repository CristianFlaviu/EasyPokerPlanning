using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Application.Abstractions.Security;

public interface IPasswordHasher
{
    PasswordHash Hash(string plaintext);
    bool Verify(string plaintext, PasswordHash hash);
}
