using PokerPlanning.Domain.Participants;

namespace PokerPlanning.Domain.Rooms;

public sealed class CompletedRound
{
    private readonly Dictionary<ParticipantId, Card> _votes = [];

    private CompletedRound()
    {
    }

    internal CompletedRound(
        Guid id,
        string? title,
        Dictionary<ParticipantId, Card> votes,
        Card? finalEstimate,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt)
    {
        Id = id;
        Title = title;
        _votes = votes;
        FinalEstimate = finalEstimate;
        StartedAt = startedAt;
        EndedAt = endedAt;
    }

    public Guid Id { get; private set; }
    public string? Title { get; private set; }
    public IReadOnlyDictionary<ParticipantId, Card> Votes => _votes.AsReadOnly();
    public Card? FinalEstimate { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset EndedAt { get; private set; }
}
