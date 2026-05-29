using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms.Events;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Domain.Rooms;

public sealed class Room : AggregateRoot
{
    public const int MinNameLength = 1;
    public const int MaxNameLength = 80;

    private readonly List<Participant> _participants = [];
    private readonly List<CompletedRound> _history = [];
    private readonly HashSet<ParticipantId> _moderatorIds = [];

    private Room(
        RoomId id,
        string name,
        PasswordHash? passwordHash,
        ParticipantId ownerId,
        UserId? ownerUserId,
        DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        PasswordHash = passwordHash;
        OwnerId = ownerId;
        OwnerUserId = ownerUserId;
        CreatedAt = createdAt;
    }

    public RoomId Id { get; }
    public string Name { get; private set; }
    public PasswordHash? PasswordHash { get; private set; }
    public ParticipantId OwnerId { get; }
    public UserId? OwnerUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public Round? CurrentRound { get; private set; }

    public IReadOnlyList<Participant> Participants => _participants.AsReadOnly();
    public IReadOnlyList<CompletedRound> History => _history.AsReadOnly();
    public IReadOnlySet<ParticipantId> ModeratorIds => _moderatorIds;

    public bool IsPasswordProtected => PasswordHash is not null;

    public bool HasParticipant(ParticipantId participantId) =>
        _participants.Any(p => p.Id == participantId);

    public bool HasUserAccess(UserId userId) =>
        OwnerUserId == userId || _participants.Any(p => p.UserId == userId);

    public ParticipantId? GetParticipantIdForUser(UserId userId)
    {
        if (OwnerUserId == userId)
            return OwnerId;

        return _participants.FirstOrDefault(p => p.UserId == userId)?.Id;
    }

    public void RestoreCurrentRound(Round? round)
    {
        CurrentRound = round;
    }

    public static Result<Room> Create(
        string name,
        PasswordHash? passwordHash,
        ParticipantId ownerId,
        string ownerDisplayName,
        DateTimeOffset now,
        UserId? ownerUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length is < MinNameLength or > MaxNameLength)
            return Result.Failure<Room>(RoomErrors.InvalidName);

        var ownerResult = Participant.Create(ownerId, ownerDisplayName, ParticipantRole.Voter, now, ownerUserId);
        if (ownerResult.IsFailure)
            return Result.Failure<Room>(ownerResult.Error);

        var room = new Room(RoomId.New(), name.Trim(), passwordHash, ownerId, ownerUserId, now);
        room._participants.Add(ownerResult.Value);
        room.RaiseDomainEvent(new RoomCreatedEvent(room.Id, ownerId, now));
        return Result.Success(room);
    }

    public Result AddParticipant(
        ParticipantId participantId,
        string displayName,
        ParticipantRole role,
        DateTimeOffset now,
        UserId? userId = null,
        bool allowExistingSeat = false)
    {
        var existing = _participants.FirstOrDefault(p => p.Id == participantId);
        if (existing is not null)
        {
            if (!allowExistingSeat)
                return Result.Failure(RoomErrors.SeatReserved);

            var renameResult = existing.Rename(displayName);
            if (renameResult.IsFailure)
                return renameResult;

            existing.SetRole(role);
            if (userId is not null)
                existing.SetUserId(userId);
            RaiseDomainEvent(new ParticipantJoinedEvent(Id, participantId, existing.DisplayName, existing.Role, existing.UserId, now));
            return Result.Success();
        }

        // Defensive guard: owner/moderator ids should already be present, but privileged
        // seats must never be claimable through a fresh join.
        if (participantId == OwnerId || _moderatorIds.Contains(participantId))
            return Result.Failure(RoomErrors.SeatReserved);

        var participantResult = Participant.Create(participantId, displayName, role, now, userId);
        if (participantResult.IsFailure)
            return Result.Failure(participantResult.Error);

        _participants.Add(participantResult.Value);
        RaiseDomainEvent(new ParticipantJoinedEvent(
            Id,
            participantId,
            participantResult.Value.DisplayName,
            participantResult.Value.Role,
            participantResult.Value.UserId,
            now));
        return Result.Success();
    }

    public Result StartRound(ParticipantId callerId, string? title, DateTimeOffset now)
    {
        if (!CanModerate(callerId))
            return Result.Failure(RoomErrors.NotAuthorized);

        if (CurrentRound is not null)
            return Result.Failure(RoundErrors.AlreadyActive);

        var roundResult = Round.Start(title, now);
        if (roundResult.IsFailure)
            return Result.Failure(roundResult.Error);

        CurrentRound = roundResult.Value;
        RaiseDomainEvent(new RoundStartedEvent(Id, CurrentRound.Id, CurrentRound.Title, now));
        return Result.Success();
    }

    public Result SubmitVote(ParticipantId participantId, Card card, DateTimeOffset now)
    {
        if (CurrentRound is null)
            return Result.Failure(RoundErrors.NotActive);

        var participant = _participants.FirstOrDefault(p => p.Id == participantId);
        if (participant is null)
            return Result.Failure(RoomErrors.ParticipantNotFound);

        if (participant.Role == ParticipantRole.Observer)
            return Result.Failure(RoomErrors.ObserverCannotVote);

        var result = CurrentRound.SubmitVote(participantId, card);
        if (result.IsFailure)
            return result;

        RaiseDomainEvent(new VoteSubmittedEvent(Id, CurrentRound.Id, participantId, now));
        return Result.Success();
    }

    public Result RevealVotes(ParticipantId callerId, DateTimeOffset now)
    {
        if (!CanModerate(callerId))
            return Result.Failure(RoomErrors.NotAuthorized);

        if (CurrentRound is null)
            return Result.Failure(RoundErrors.NotActive);

        var result = CurrentRound.Reveal();
        if (result.IsFailure)
            return result;

        RaiseDomainEvent(new VotesRevealedEvent(
            Id,
            CurrentRound.Id,
            CurrentRound.Votes,
            now));
        return Result.Success();
    }

    public Result ResetCurrentRound(ParticipantId callerId, DateTimeOffset now)
    {
        if (!CanModerate(callerId))
            return Result.Failure(RoomErrors.NotAuthorized);

        if (CurrentRound is null)
            return Result.Failure(RoundErrors.NotActive);

        var roundId = CurrentRound.Id;
        var result = CurrentRound.Reset();
        if (result.IsFailure)
            return result;

        RaiseDomainEvent(new RoundResetEvent(Id, roundId, now));
        return Result.Success();
    }

    public Result EndRound(ParticipantId callerId, Card? finalEstimate, DateTimeOffset now)
    {
        if (!CanModerate(callerId))
            return Result.Failure(RoomErrors.NotAuthorized);

        if (CurrentRound is null)
            return Result.Failure(RoundErrors.NotActive);

        var completedResult = CurrentRound.Complete(finalEstimate, now);
        if (completedResult.IsFailure)
            return Result.Failure(completedResult.Error);

        var roundId = CurrentRound.Id;
        _history.Add(completedResult.Value);
        CurrentRound = null;
        RaiseDomainEvent(new RoundEndedEvent(Id, roundId, finalEstimate, now));
        return Result.Success();
    }

    public Result PromoteToModerator(ParticipantId callerId, ParticipantId participantId, DateTimeOffset now)
    {
        if (callerId != OwnerId)
            return Result.Failure(RoomErrors.NotAuthorized);

        if (_participants.All(p => p.Id != participantId))
            return Result.Failure(RoomErrors.ParticipantNotFound);

        if (participantId == OwnerId || !_moderatorIds.Add(participantId))
            return Result.Success();

        RaiseDomainEvent(new ModeratorPromotedEvent(Id, participantId, now));
        return Result.Success();
    }

    public Result DemoteModerator(ParticipantId callerId, ParticipantId participantId, DateTimeOffset now)
    {
        if (callerId != OwnerId)
            return Result.Failure(RoomErrors.NotAuthorized);

        if (_participants.All(p => p.Id != participantId))
            return Result.Failure(RoomErrors.ParticipantNotFound);

        if (!_moderatorIds.Remove(participantId))
            return Result.Success();

        RaiseDomainEvent(new ModeratorDemotedEvent(Id, participantId, now));
        return Result.Success();
    }

    public Result ChangeRole(ParticipantId participantId, ParticipantRole role, DateTimeOffset now)
    {
        var participant = _participants.FirstOrDefault(p => p.Id == participantId);
        if (participant is null)
            return Result.Failure(RoomErrors.ParticipantNotFound);

        participant.SetRole(role);
        RaiseDomainEvent(new ParticipantRoleChangedEvent(Id, participantId, role, now));
        return Result.Success();
    }

    public ParticipantId? UpdateParticipantForUser(UserId userId, string displayName)
    {
        var participant = _participants.FirstOrDefault(p => p.UserId == userId);
        if (participant is null)
            return null;

        var result = participant.Rename(displayName);
        return result.IsSuccess ? participant.Id : null;
    }

    public Result LeaveRoom(ParticipantId participantId, DateTimeOffset now)
    {
        if (participantId == OwnerId)
            return Result.Failure(RoomErrors.OwnerCannotLeave);

        return RemoveParticipantFromTable(participantId, now);
    }

    public Result RemoveParticipant(ParticipantId callerId, ParticipantId participantId, DateTimeOffset now)
    {
        if (!CanModerate(callerId))
            return Result.Failure(RoomErrors.NotAuthorized);

        if (participantId == OwnerId)
            return Result.Failure(RoomErrors.OwnerCannotBeRemoved);

        return RemoveParticipantFromTable(participantId, now);
    }

    private Result RemoveParticipantFromTable(ParticipantId participantId, DateTimeOffset now)
    {
        var participant = _participants.FirstOrDefault(p => p.Id == participantId);
        if (participant is null)
            return Result.Failure(RoomErrors.ParticipantNotFound);

        _participants.Remove(participant);
        _moderatorIds.Remove(participantId);
        CurrentRound?.RemoveVote(participantId);

        RaiseDomainEvent(new ParticipantLeftEvent(Id, participantId, now));
        return Result.Success();
    }

    private bool CanModerate(ParticipantId participantId) =>
        OwnerId == participantId || _moderatorIds.Contains(participantId);
}

public static class RoomErrors
{
    public static readonly Error InvalidName = new(
        "Room.InvalidName",
        $"Room name must be {Room.MinNameLength}-{Room.MaxNameLength} characters.");

    public static readonly Error NotFound = new(
        "Room.NotFound",
        "Room not found.");

    public static readonly Error InvalidPassword = new(
        "Room.InvalidPassword",
        "Room password is incorrect.");

    public static readonly Error NotAuthorized = new(
        "Room.NotAuthorized",
        "Only the room owner or a moderator can perform this action.");

    public static readonly Error ParticipantNotFound = new(
        "Room.ParticipantNotFound",
        "Participant is not in this room.");

    public static readonly Error ObserverCannotVote = new(
        "Room.ObserverCannotVote",
        "Observers cannot submit votes.");

    public static readonly Error OwnerCannotLeave = new(
        "Room.OwnerCannotLeave",
        "The room owner cannot leave an active room.");

    public static readonly Error OwnerCannotBeRemoved = new(
        "Room.OwnerCannotBeRemoved",
        "The room owner cannot be removed from the table.");

    public static readonly Error SeatReserved = new(
        "Room.SeatReserved",
        "This seat is reserved and cannot be joined.");
}
