using MediatR;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Security;
using PokerPlanning.Application.Abstractions.Time;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Application.Features.CreateRoom;

public sealed class CreateRoomHandler(
    IRoomRepository rooms,
    IPasswordHasher passwordHasher,
    IRoomAccessTokenService accessTokens,
    IClock clock)
    : IRequestHandler<CreateRoomCommand, Result<CreateRoomResult>>
{
    public async Task<Result<CreateRoomResult>> Handle(CreateRoomCommand cmd, CancellationToken ct)
    {
        PasswordHash? hash = string.IsNullOrEmpty(cmd.Password)
            ? null
            : passwordHasher.Hash(cmd.Password);

        var ownerId = new ParticipantId(cmd.OwnerParticipantId);
        UserId? ownerUserId = cmd.OwnerUserId is { } id ? new UserId(id) : null;

        var roomResult = Room.Create(
            cmd.Name,
            hash,
            ownerId,
            cmd.OwnerDisplayName,
            clock.UtcNow,
            ownerUserId);

        if (roomResult.IsFailure)
            return Result.Failure<CreateRoomResult>(roomResult.Error);

        var room = roomResult.Value;
        await rooms.AddAsync(room, ct);
        await rooms.SaveChangesAsync(ct);

        var accessToken = accessTokens.Issue(room.Id, ownerId);
        return Result.Success(new CreateRoomResult(room.Id.Value, ownerId.Value, accessToken));
    }
}
