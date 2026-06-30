using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupAlbumMetadata.Lookup;

public interface IGetAlbumMetadata
{
    Task<AlbumMetadata?> GetMetadataAsync(
        string artistName,
        string albumTitle,
        string? sourceAlbumId,
        string? sourceArtistId,
        CancellationToken cancellationToken);
}
