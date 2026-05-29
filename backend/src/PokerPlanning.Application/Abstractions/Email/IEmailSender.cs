namespace PokerPlanning.Application.Abstractions.Email;

public interface IEmailSender
{
    Task SendMagicLinkAsync(
        string toEmail,
        string displayName,
        string loginUrl,
        DateTimeOffset expiresAt,
        CancellationToken ct);
}
