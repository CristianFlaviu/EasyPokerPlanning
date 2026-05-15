namespace PokerPlanning.Api.Hubs;

public interface IRoomClient
{
    Task ParticipantJoined(ParticipantJoinedMessage participant);
    Task RoundStarted(RoundStartedMessage round);
    Task VoteSubmitted(VoteSubmittedMessage vote);
    Task VotesRevealed(VotesRevealedMessage votes);
    Task RoundReset(RoundResetMessage round);
    Task RoundEnded(RoundEndedMessage round);
    Task ModeratorPromoted(ModeratorChangedMessage moderator);
    Task ModeratorDemoted(ModeratorChangedMessage moderator);
    Task ParticipantRoleChanged(ParticipantRoleChangedMessage participant);
}

public sealed record ParticipantJoinedMessage(
    Guid Id,
    string DisplayName,
    string Role);

public sealed record RoundStartedMessage(
    Guid Id,
    string? Title,
    string Phase);

public sealed record VoteSubmittedMessage(
    Guid RoundId,
    Guid ParticipantId);

public sealed record VotesRevealedMessage(
    Guid RoundId,
    IReadOnlyList<RevealedVoteMessage> Votes);

public sealed record RevealedVoteMessage(
    Guid ParticipantId,
    string Card);

public sealed record RoundResetMessage(Guid RoundId);

public sealed record RoundEndedMessage(
    Guid RoundId,
    string? FinalEstimate);

public sealed record ModeratorChangedMessage(Guid ParticipantId);

public sealed record ParticipantRoleChangedMessage(
    Guid ParticipantId,
    string Role);
