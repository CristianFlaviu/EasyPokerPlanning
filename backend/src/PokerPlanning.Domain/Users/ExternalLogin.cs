namespace PokerPlanning.Domain.Users;

public sealed record ExternalLogin(string Provider, string Subject)
{
    public const string GoogleProvider = "google";
}
