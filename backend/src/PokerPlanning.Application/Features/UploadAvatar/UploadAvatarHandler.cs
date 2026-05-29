using MediatR;
using PokerPlanning.Application.Abstractions.Storage;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Application.Features.UploadAvatar;

public sealed class UploadAvatarHandler(IAvatarStorage avatars)
    : IRequestHandler<UploadAvatarCommand, Result<UploadAvatarResult>>
{
    public async Task<Result<UploadAvatarResult>> Handle(UploadAvatarCommand cmd, CancellationToken ct)
    {
        var url = await avatars.UploadAsync(cmd.Content, cmd.ContentType, new UserId(cmd.UserId), ct);
        return Result.Success(new UploadAvatarResult(url));
    }
}
