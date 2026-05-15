using PokerPlanning.Domain.Common;

namespace PokerPlanning.Domain.Participants;

public sealed class Participant
{
    public const int MaxDisplayNameLength = 40;

    private Participant(ParticipantId id, string displayName, ParticipantRole role, DateTimeOffset joinedAt)
    {
        Id = id;
        DisplayName = displayName;
        Role = role;
        JoinedAt = joinedAt;
    }

    public ParticipantId Id { get; }
    public string DisplayName { get; private set; }
    public ParticipantRole Role { get; private set; }
    public DateTimeOffset JoinedAt { get; }

    public static Result<Participant> Create(
        ParticipantId id,
        string displayName,
        ParticipantRole role,
        DateTimeOffset joinedAt)
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > MaxDisplayNameLength)
            return Result.Failure<Participant>(ParticipantErrors.InvalidDisplayName);

        return Result.Success(new Participant(id, displayName.Trim(), role, joinedAt));
    }

    public void SetRole(ParticipantRole role) => Role = role;

    internal Result Rename(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > MaxDisplayNameLength)
            return Result.Failure(ParticipantErrors.InvalidDisplayName);

        DisplayName = displayName.Trim();
        return Result.Success();
    }
}

public static class ParticipantErrors
{
    public static readonly Error InvalidDisplayName = new(
        "Participant.InvalidDisplayName",
        $"Display name must be 1-{Participant.MaxDisplayNameLength} characters.");
}
