using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms.Events;

namespace PokerPlanning.Domain.Rooms;

public sealed class Room : AggregateRoot
{
    public const int MinNameLength = 1;
    public const int MaxNameLength = 80;

    private readonly List<Participant> _participants = [];
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

    public IReadOnlyList<Participant> Participants => _participants.AsReadOnly();
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
}
