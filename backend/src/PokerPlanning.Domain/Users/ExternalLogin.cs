namespace PokerPlanning.Domain.Users;

public sealed record ExternalLogin(string Provider, string Subject)
{
    public const int MaxProviderLength = 64;
    public const int MaxSubjectLength = 254;

    public const string GoogleProvider = "google";
    public const string EmailProvider = "email";
}
