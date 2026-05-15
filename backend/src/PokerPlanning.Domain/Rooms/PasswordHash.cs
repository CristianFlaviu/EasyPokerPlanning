namespace PokerPlanning.Domain.Rooms;

public readonly record struct PasswordHash(string Value)
{
    public override string ToString() => "***";
}
