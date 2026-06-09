namespace PokerPlanning.Application.Features.ThrowReaction;

// The fixed "throwable" palette. Reactions are purely cosmetic and ephemeral, so the
// allowed set is a small, fixed list validated on the server to keep payloads sane.
public static class ReactionEmojis
{
    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>
    {
        "\U0001F345", // 🍅 tomato
        "☕",     // ☕ coffee
        "❤️", // ❤️ heart
        "\U0001F389", // 🎉 party
        "\U0001F4A9", // 💩 poop
        "\U0001F44F", // 👏 clap
        "\U0001F44D", // 👍 thumbs up
        "\U0001F440", // 👀 eyes
    };
}
