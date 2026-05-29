using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using PokerPlanning.Application.Abstractions.Email;

namespace PokerPlanning.Infrastructure.Email;

public sealed class GmailSmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendMagicLinkAsync(
        string toEmail,
        string displayName,
        string loginUrl,
        DateTimeOffset expiresAt,
        CancellationToken ct)
    {
        var options = ReadOptions();
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(options.FromName, options.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Your Easy Poker login link";

        var safeDisplayName = string.IsNullOrWhiteSpace(displayName) ? toEmail : displayName.Trim();
        message.Body = new BodyBuilder
        {
            TextBody = $"""
                Hi {safeDisplayName},

                Use this link to sign in to Easy Poker:
                {loginUrl}

                This link expires at {expiresAt:O} and can be used once.
                """,
            HtmlBody = $"""
                <p>Hi {System.Net.WebUtility.HtmlEncode(safeDisplayName)},</p>
                <p>Use this link to sign in to Easy Poker:</p>
                <p><a href="{System.Net.WebUtility.HtmlEncode(loginUrl)}">Sign in to Easy Poker</a></p>
                <p>This link expires at {expiresAt:O} and can be used once.</p>
                """,
        }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(options.Host, options.Port, SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(options.UserName, options.Password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(quit: true, ct);
    }

    private SmtpOptions ReadOptions()
    {
        var section = configuration.GetSection("Email:Smtp");
        var host = section["Host"] ?? "smtp.gmail.com";
        var port = int.TryParse(section["Port"], out var parsedPort) ? parsedPort : 587;
        var fromEmail = section["FromEmail"];
        var fromName = section["FromName"] ?? "Easy Poker";
        var userName = section["UserName"] ?? fromEmail;
        var password = section["Password"];

        if (string.IsNullOrWhiteSpace(fromEmail) ||
            string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Email:Smtp:FromEmail, Email:Smtp:UserName, and Email:Smtp:Password must be configured.");
        }

        return new SmtpOptions(host, port, fromEmail, fromName, userName, password);
    }

    private sealed record SmtpOptions(
        string Host,
        int Port,
        string FromEmail,
        string FromName,
        string UserName,
        string Password);
}
