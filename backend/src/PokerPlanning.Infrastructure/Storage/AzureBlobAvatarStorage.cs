using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using PokerPlanning.Application.Abstractions.Storage;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Infrastructure.Storage;

public sealed class AzureBlobAvatarStorage(IConfiguration configuration) : IAvatarStorage
{
    private const string DefaultContainerName = "avatars";

    public async Task<string> UploadAsync(Stream content, string contentType, UserId userId, CancellationToken ct)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException("AzureStorage:ConnectionString must be configured.");
        var containerName = configuration["AzureStorage:AvatarsContainer"] ?? DefaultContainerName;

        var container = new BlobServiceClient(connectionString).GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var blobName = $"{userId.Value}/{Guid.NewGuid():N}{ExtensionFor(contentType)}";
        var blob = container.GetBlobClient(blobName);

        if (content.CanSeek)
            content.Position = 0;

        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            ct);

        return blob.Uri.ToString();
    }

    private static string ExtensionFor(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => string.Empty,
    };
}
