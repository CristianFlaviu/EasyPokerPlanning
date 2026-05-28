using MediatR;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Security;
using PokerPlanning.Application.Abstractions.Time;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Application.Features.JoinRoom;

public sealed class JoinRoomHandler(
    IRoomRepository rooms,
    IPasswordHasher passwordHasher,
    IClock clock)
    : IRequestHandler<JoinRoomCommand, Result<JoinRoomResult>>
{
    public async Task<Result<JoinRoomResult>> Handle(JoinRoomCommand cmd, CancellationToken ct)
    {
        var roomId = new RoomId(cmd.RoomId);
        var room = await rooms.GetByIdAsync(roomId, ct);
        if (room is null)
            return Result.Failure<JoinRoomResult>(RoomErrors.NotFound);

        if (room.PasswordHash is not null
            && (string.IsNullOrEmpty(cmd.Password) || !passwordHasher.Verify(cmd.Password, room.PasswordHash.Value)))
        {
            return Result.Failure<JoinRoomResult>(RoomErrors.InvalidPassword);
        }

        var participantId = new ParticipantId(cmd.ParticipantId);
        UserId? callerUserId = cmd.CallerUserId is { } id ? new UserId(id) : null;
        var joinResult = room.AddParticipant(participantId, cmd.DisplayName, cmd.Role, clock.UtcNow, callerUserId);
        if (joinResult.IsFailure)
            return Result.Failure<JoinRoomResult>(joinResult.Error);

        await rooms.SaveChangesAsync(ct);
        return Result.Success(new JoinRoomResult(room.Id.Value, participantId.Value));
    }
}
