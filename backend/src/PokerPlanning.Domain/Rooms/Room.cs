using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms.Events;

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
        DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        PasswordHash = passwordHash;
        OwnerId = ownerId;
        CreatedAt = createdAt;
    }

    public RoomId Id { get; }
    public string Name { get; private set; }
    public PasswordHash? PasswordHash { get; private set; }
    public ParticipantId OwnerId { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public Round? CurrentRound { get; private set; }

    public IReadOnlyList<Participant> Participants => _participants.AsReadOnly();
    public IReadOnlyList<CompletedRound> History => _history.AsReadOnly();
    public IReadOnlySet<ParticipantId> ModeratorIds => _moderatorIds;

    public bool IsPasswordProtected => PasswordHash is not null;

    public static Result<Room> Create(
        string name,
        PasswordHash? passwordHash,
        ParticipantId ownerId,
        string ownerDisplayName,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length is < MinNameLength or > MaxNameLength)
            return Result.Failure<Room>(RoomErrors.InvalidName);

        var ownerResult = Participant.Create(ownerId, ownerDisplayName, ParticipantRole.Voter, now);
        if (ownerResult.IsFailure)
            return Result.Failure<Room>(ownerResult.Error);

        var room = new Room(RoomId.New(), name.Trim(), passwordHash, ownerId, now);
        room._participants.Add(ownerResult.Value);
        room.RaiseDomainEvent(new RoomCreatedEvent(room.Id, ownerId, now));
        return Result.Success(room);
    }

    public Result AddParticipant(
        ParticipantId participantId,
        string displayName,
        ParticipantRole role,
        DateTimeOffset now)
    {
        var existing = _participants.FirstOrDefault(p => p.Id == participantId);
        if (existing is not null)
        {
            var renameResult = existing.Rename(displayName);
            if (renameResult.IsFailure)
                return renameResult;

            existing.SetRole(role);
            RaiseDomainEvent(new ParticipantJoinedEvent(Id, participantId, existing.DisplayName, existing.Role, now));
            return Result.Success();
        }

        var participantResult = Participant.Create(participantId, displayName, role, now);
        if (participantResult.IsFailure)
            return Result.Failure(participantResult.Error);

        _participants.Add(participantResult.Value);
        RaiseDomainEvent(new ParticipantJoinedEvent(
            Id,
            participantId,
            participantResult.Value.DisplayName,
            participantResult.Value.Role,
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
}
