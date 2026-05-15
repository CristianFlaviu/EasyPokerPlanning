using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Infrastructure.Persistence.Configurations;

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("rooms");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => new RoomId(value))
            .HasColumnName("id");

        builder.Property(r => r.Name)
            .HasColumnName("name")
            .HasMaxLength(Room.MaxNameLength)
            .IsRequired();

        builder.Property(r => r.OwnerId)
            .HasConversion(id => id.Value, value => new ParticipantId(value))
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(r => r.PasswordHash)
            .HasConversion(
                hash => hash == null ? null : hash.Value.Value,
                value => value == null ? null : new PasswordHash(value))
            .HasColumnName("password_hash");

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(r => r.ArchivedAt)
            .HasColumnName("archived_at");

        builder.OwnsMany(r => r.Participants, pb =>
        {
            pb.ToTable("room_participants");
            pb.WithOwner().HasForeignKey("room_id");

            pb.Property<int>("ordinal");
            pb.HasKey("room_id", "ordinal");

            pb.Property(p => p.Id)
                .HasConversion(id => id.Value, value => new ParticipantId(value))
                .HasColumnName("participant_id")
                .IsRequired();

            pb.Property(p => p.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(Participant.MaxDisplayNameLength)
                .IsRequired();

            pb.Property(p => p.Role)
                .HasColumnName("role")
                .HasConversion<int>()
                .IsRequired();

            pb.Property(p => p.JoinedAt)
                .HasColumnName("joined_at")
                .IsRequired();
        });

        builder.Ignore(r => r.ModeratorIds);
        builder.Ignore(r => r.DomainEvents);
    }
}
