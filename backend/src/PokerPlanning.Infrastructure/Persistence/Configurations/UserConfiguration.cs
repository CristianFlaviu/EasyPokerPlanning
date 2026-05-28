using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasConversion(id => id.Value, value => new UserId(value))
            .HasColumnName("id");

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(User.MaxEmailLength)
            .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(User.MaxDisplayNameLength)
            .IsRequired();

        builder.Property(u => u.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasMaxLength(User.MaxAvatarUrlLength);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("last_login_at")
            .IsRequired();

        builder.Property<List<ExternalLogin>>("_logins")
            .HasColumnName("logins")
            .HasConversion(
                logins => SerializeLogins(logins),
                value => DeserializeLogins(value))
            .Metadata.SetValueComparer(LoginsComparer);

        builder.Ignore(u => u.Logins);
        builder.Ignore(u => u.DomainEvents);
    }

    private static readonly ValueComparer<List<ExternalLogin>> LoginsComparer = new(
        (left, right) => LoginsEqual(left, right),
        logins => SerializeLogins(logins).GetHashCode(),
        logins => DeserializeLogins(SerializeLogins(logins)));

    private static bool LoginsEqual(List<ExternalLogin>? left, List<ExternalLogin>? right) =>
        SerializeLogins(left ?? []) == SerializeLogins(right ?? []);

    private static string SerializeLogins(List<ExternalLogin> logins)
    {
        var jsonShape = logins
            .Select(l => new LoginJson(l.Provider, l.Subject))
            .OrderBy(l => l.Provider, StringComparer.Ordinal)
            .ThenBy(l => l.Subject, StringComparer.Ordinal)
            .ToList();
        return JsonSerializer.Serialize(jsonShape);
    }

    private static List<ExternalLogin> DeserializeLogins(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        var jsonShape = JsonSerializer.Deserialize<List<LoginJson>>(json) ?? [];
        return jsonShape.Select(l => new ExternalLogin(l.Provider, l.Subject)).ToList();
    }

    private sealed record LoginJson(string Provider, string Subject);
}
