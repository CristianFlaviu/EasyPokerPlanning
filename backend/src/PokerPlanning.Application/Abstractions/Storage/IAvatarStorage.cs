using PokerPlanning.Domain.Users;

namespace PokerPlanning.Application.Abstractions.Storage;

public interface IAvatarStorage
{
    Task<string> UploadAsync(Stream content, string contentType, UserId userId, CancellationToken ct);
}
