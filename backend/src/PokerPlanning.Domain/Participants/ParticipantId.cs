namespace PokerPlanning.Domain.Participants;

public readonly record struct ParticipantId(Guid Value)
{
    public static ParticipantId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
