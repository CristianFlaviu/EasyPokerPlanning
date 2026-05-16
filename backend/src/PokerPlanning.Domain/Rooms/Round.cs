using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;

namespace PokerPlanning.Domain.Rooms;

public sealed class Round
{
    public const int MaxTitleLength = 200;

    private readonly Dictionary<ParticipantId, Card> _votes = [];

    private Round()
    {
    }

    private Round(Guid id, string? title, DateTimeOffset startedAt)
    {
        Id = id;
        Title = title;
        Phase = RoundPhase.Voting;
        StartedAt = startedAt;
    }

    public Guid Id { get; private set; }
    public string? Title { get; private set; }
    public RoundPhase Phase { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public IReadOnlyDictionary<ParticipantId, Card> Votes => _votes.AsReadOnly();

    public static Result<Round> Start(string? title, DateTimeOffset now)
    {
        var normalizedTitle = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        if (normalizedTitle?.Length > MaxTitleLength)
            return Result.Failure<Round>(RoundErrors.InvalidTitle);

        return Result.Success(new Round(Guid.NewGuid(), normalizedTitle, now));
    }

    public static Result<Round> Restore(
        Guid id,
        string? title,
        RoundPhase phase,
        DateTimeOffset startedAt,
        IReadOnlyDictionary<ParticipantId, Card> votes)
    {
        var normalizedTitle = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        if (normalizedTitle?.Length > MaxTitleLength)
            return Result.Failure<Round>(RoundErrors.InvalidTitle);

        var round = new Round(id, normalizedTitle, startedAt)
        {
            Phase = phase
        };

        foreach (var vote in votes)
        {
            round._votes[vote.Key] = vote.Value;
        }

        return Result.Success(round);
    }

    internal Result SubmitVote(ParticipantId participantId, Card card)
    {
        if (Phase != RoundPhase.Voting)
            return Result.Failure(RoundErrors.VotingNotOpen);

        _votes[participantId] = card;
        return Result.Success();
    }

    internal void RemoveVote(ParticipantId participantId)
    {
        _votes.Remove(participantId);
    }

    internal Result Reveal()
    {
        if (Phase != RoundPhase.Voting)
            return Result.Failure(RoundErrors.CannotReveal);

        if (_votes.Count == 0)
            return Result.Failure(RoundErrors.EmptyRound);

        Phase = RoundPhase.Revealed;
        return Result.Success();
    }

    internal Result Reset()
    {
        if (Phase != RoundPhase.Revealed)
            return Result.Failure(RoundErrors.CannotReset);

        _votes.Clear();
        Phase = RoundPhase.Voting;
        return Result.Success();
    }

    internal Result<CompletedRound> Complete(Card? finalEstimate, DateTimeOffset endedAt)
    {
        if (Phase != RoundPhase.Revealed)
            return Result.Failure<CompletedRound>(RoundErrors.CannotEnd);

        if (_votes.Count == 0)
            return Result.Failure<CompletedRound>(RoundErrors.EmptyRound);

        return Result.Success(new CompletedRound(
            Id,
            Title,
            new Dictionary<ParticipantId, Card>(_votes),
            finalEstimate,
            StartedAt,
            endedAt));
    }
}

public static class RoundErrors
{
    public static readonly Error InvalidTitle = new(
        "Round.InvalidTitle",
        $"Round title must be {Round.MaxTitleLength} characters or fewer.");

    public static readonly Error AlreadyActive = new(
        "Round.AlreadyActive",
        "A round is already active.");

    public static readonly Error NotActive = new(
        "Round.NotActive",
        "No round is currently active.");

    public static readonly Error VotingNotOpen = new(
        "Round.VotingNotOpen",
        "Votes can only be submitted while the round is voting.");

    public static readonly Error CannotReveal = new(
        "Round.CannotReveal",
        "Only a voting round can be revealed.");

    public static readonly Error EmptyRound = new(
        "Round.EmptyRound",
        "A round must have at least one vote before it can be revealed or ended.");

    public static readonly Error CannotReset = new(
        "Round.CannotReset",
        "Only a revealed round can be reset.");

    public static readonly Error CannotEnd = new(
        "Round.CannotEnd",
        "Only a revealed round can be ended.");
}
