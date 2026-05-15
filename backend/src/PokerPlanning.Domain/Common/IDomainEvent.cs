namespace PokerPlanning.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
