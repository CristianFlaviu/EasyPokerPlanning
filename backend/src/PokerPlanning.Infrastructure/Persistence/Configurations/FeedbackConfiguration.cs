using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FeedbackEntity = PokerPlanning.Domain.Feedback.Feedback;

namespace PokerPlanning.Infrastructure.Persistence.Configurations;

public sealed class FeedbackConfiguration : IEntityTypeConfiguration<FeedbackEntity>
{
    public void Configure(EntityTypeBuilder<FeedbackEntity> builder)
    {
        builder.ToTable("feedback");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnName("id");

        builder.Property(f => f.Name)
            .HasColumnName("name")
            .HasMaxLength(FeedbackEntity.MaxNameLength);

        builder.Property(f => f.Email)
            .HasColumnName("email")
            .HasMaxLength(FeedbackEntity.MaxEmailLength);

        builder.Property(f => f.Message)
            .HasColumnName("message")
            .HasMaxLength(FeedbackEntity.MaxMessageLength)
            .IsRequired();

        builder.Property(f => f.UserId)
            .HasColumnName("user_id");

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(f => f.CreatedAt);
    }
}
