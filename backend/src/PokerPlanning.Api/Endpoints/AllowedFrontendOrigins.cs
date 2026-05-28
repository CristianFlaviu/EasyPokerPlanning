namespace PokerPlanning.Api.Endpoints;

public sealed record AllowedFrontendOrigins(
    IReadOnlyList<string> Exact,
    IReadOnlyList<string> Wildcard);
