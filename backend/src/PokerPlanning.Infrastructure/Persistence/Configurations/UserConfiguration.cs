using Microsoft.EntityFrameworkCore;
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

        builder.OwnsMany<ExternalLogin>("_logins", login =>
        {
            login.ToTable("user_logins");
            login.WithOwner().HasForeignKey("user_id");

            login.Property(l => l.Provider)
                .HasColumnName("provider")
                .HasMaxLength(ExternalLogin.MaxProviderLength)
                .IsRequired();

            login.Property(l => l.Subject)
                .HasColumnName("subject")
                .HasMaxLength(ExternalLogin.MaxSubjectLength)
                .IsRequired();

            login.HasKey("user_id", nameof(ExternalLogin.Provider), nameof(ExternalLogin.Subject));
            login.HasIndex(l => new { l.Provider, l.Subject }).IsUnique();
        });

        builder.Ignore(u => u.Logins);
        builder.Ignore(u => u.DomainEvents);
        builder.Navigation("_logins").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
