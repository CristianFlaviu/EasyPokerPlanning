using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Infrastructure.Persistence.Configurations;

public sealed class EmailLoginTokenConfiguration : IEntityTypeConfiguration<EmailLoginToken>
{
    public void Configure(EntityTypeBuilder<EmailLoginToken> builder)
    {
        builder.ToTable("email_login_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id");

        builder.Property(t => t.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(EmailLoginToken.TokenHashLength)
            .IsRequired();

        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.Property(t => t.Email)
            .HasColumnName("email")
            .HasMaxLength(User.MaxEmailLength)
            .IsRequired();

        builder.Property(t => t.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(User.MaxDisplayNameLength);

        builder.Property(t => t.Mode)
            .HasColumnName("mode")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(t => t.ReturnUrl)
            .HasColumnName("return_url")
            .HasMaxLength(EmailLoginToken.MaxReturnUrlLength)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(t => t.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(t => t.ConsumedAt)
            .HasColumnName("consumed_at");
    }
}
