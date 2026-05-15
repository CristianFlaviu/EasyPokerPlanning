using PokerPlanning.Domain.Common;

namespace PokerPlanning.Domain.Rooms;

public readonly record struct Card
{
    public const string Unknown = "?";
    public static readonly string[] AllowedValues = ["1", "2", "3", "5", "8", "13", "21", Unknown];

    public string Value { get; }

    private Card(string value) => Value = value;

    public static Result<Card> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || Array.IndexOf(AllowedValues, value) < 0)
            return Result.Failure<Card>(CardErrors.InvalidValue);

        return Result.Success(new Card(value));
    }

    public override string ToString() => Value;
}

public static class CardErrors
{
    public static readonly Error InvalidValue = new(
        "Card.InvalidValue",
        $"Card value must be one of: {string.Join(", ", Card.AllowedValues)}.");
}
