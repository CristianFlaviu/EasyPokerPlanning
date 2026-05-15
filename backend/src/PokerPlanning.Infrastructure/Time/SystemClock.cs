using PokerPlanning.Application.Abstractions.Time;

namespace PokerPlanning.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
