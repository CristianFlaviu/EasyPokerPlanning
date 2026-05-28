using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Domain.Participants;

public sealed class Participant
{
    public const int MaxDisplayNameLength = 40;

    private Participant(
        ParticipantId id,
        string displayName,
        ParticipantRole role,
        DateTimeOffset joinedAt,
        UserId? userId)
    {
        Id = id;
        DisplayName = displayName;
        Role = role;
        JoinedAt = joinedAt;
        UserId = userId;
    }

    public ParticipantId Id { get; }
    public string DisplayName { get; private set; }
    public ParticipantRole Role { get; private set; }
    public DateTimeOffset JoinedAt { get; }
    public UserId? UserId { get; private set; }

    public static Result<Participant> Create(
        ParticipantId id,
        string displayName,
        ParticipantRole role,
        DateTimeOffset joinedAt,
        UserId? userId = null)
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > MaxDisplayNameLength)
            return Result.Failure<Participant>(ParticipantErrors.InvalidDisplayName);

        return Result.Success(new Participant(id, displayName.Trim(), role, joinedAt, userId));
    }

    public void SetRole(ParticipantRole role) => Role = role;

    internal void SetUserId(UserId? userId) => UserId = userId;

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
