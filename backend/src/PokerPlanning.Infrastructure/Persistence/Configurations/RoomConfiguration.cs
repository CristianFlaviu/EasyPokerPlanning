using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        builder.Property<HashSet<ParticipantId>>("_moderatorIds")
            .HasColumnName("moderator_ids")
            .HasConversion(
                moderatorIds => SerializeParticipantIds(moderatorIds),
                value => DeserializeParticipantIds(value))
            .Metadata.SetValueComparer(ParticipantIdSetComparer);

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

        builder.OwnsOne(r => r.CurrentRound, rb =>
        {
            rb.Property(r => r.Id)
                .HasColumnName("current_round_id");

            rb.Property(r => r.Title)
                .HasColumnName("current_round_title")
                .HasMaxLength(Round.MaxTitleLength);

            rb.Property(r => r.Phase)
                .HasColumnName("current_round_phase")
                .HasConversion<int>();

            rb.Property(r => r.StartedAt)
                .HasColumnName("current_round_started_at");

            rb.Property<Dictionary<ParticipantId, Card>>("_votes")
                .HasColumnName("current_round_votes")
                .HasConversion(
                    votes => SerializeVotes(votes),
                    value => DeserializeVotes(value))
                .Metadata.SetValueComparer(VotesComparer);
        });

        builder.OwnsMany(r => r.History, hb =>
        {
            hb.ToTable("completed_rounds");
            hb.WithOwner().HasForeignKey("room_id");

            hb.Property<int>("ordinal");
            hb.HasKey("room_id", "ordinal");

            hb.Property(r => r.Id)
                .HasColumnName("round_id")
                .IsRequired();

            hb.Property(r => r.Title)
                .HasColumnName("title")
                .HasMaxLength(Round.MaxTitleLength);

            hb.Property<Dictionary<ParticipantId, Card>>("_votes")
                .HasColumnName("votes")
                .HasConversion(
                    votes => SerializeVotes(votes),
                    value => DeserializeVotes(value))
                .Metadata.SetValueComparer(VotesComparer);

            hb.Property(r => r.FinalEstimate)
                .HasConversion(
                    card => card == null ? null : card.Value.Value,
                    value => value == null ? null : Card.Create(value).Value)
                .HasColumnName("final_estimate");

            hb.Property(r => r.StartedAt)
                .HasColumnName("started_at")
                .IsRequired();

            hb.Property(r => r.EndedAt)
                .HasColumnName("ended_at")
                .IsRequired();
        });

        builder.Ignore(r => r.DomainEvents);
    }

    private static readonly ValueComparer<Dictionary<ParticipantId, Card>> VotesComparer = new(
        (left, right) => VotesEqual(left, right),
        votes => SerializeVotes(votes).GetHashCode(),
        votes => DeserializeVotes(SerializeVotes(votes)));

    private static readonly ValueComparer<HashSet<ParticipantId>> ParticipantIdSetComparer = new(
        (left, right) => ParticipantIdsEqual(left, right),
        participantIds => SerializeParticipantIds(participantIds).GetHashCode(),
        participantIds => DeserializeParticipantIds(SerializeParticipantIds(participantIds)));

    private static bool VotesEqual(
        Dictionary<ParticipantId, Card>? left,
        Dictionary<ParticipantId, Card>? right) =>
        SerializeVotes(left ?? new Dictionary<ParticipantId, Card>())
            == SerializeVotes(right ?? new Dictionary<ParticipantId, Card>());

    private static string SerializeVotes(Dictionary<ParticipantId, Card> votes)
    {
        var jsonShape = votes.ToDictionary(v => v.Key.Value, v => v.Value.Value);
        return JsonSerializer.Serialize(jsonShape);
    }

    private static Dictionary<ParticipantId, Card> DeserializeVotes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        var jsonShape = JsonSerializer.Deserialize<Dictionary<Guid, string>>(json) ?? [];
        return jsonShape.ToDictionary(
            v => new ParticipantId(v.Key),
            v => Card.Create(v.Value).Value);
    }

    private static bool ParticipantIdsEqual(
        HashSet<ParticipantId>? left,
        HashSet<ParticipantId>? right) =>
        SerializeParticipantIds(left ?? [])
            == SerializeParticipantIds(right ?? []);

    private static string SerializeParticipantIds(HashSet<ParticipantId> participantIds)
    {
        var jsonShape = participantIds.Select(id => id.Value).OrderBy(id => id).ToList();
        return JsonSerializer.Serialize(jsonShape);
    }

    private static HashSet<ParticipantId> DeserializeParticipantIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        var jsonShape = JsonSerializer.Deserialize<List<Guid>>(json) ?? [];
        return jsonShape.Select(id => new ParticipantId(id)).ToHashSet();
    }
}
