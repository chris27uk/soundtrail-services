using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupArtistMetadata.Lookup;

public interface IGetArtistMetadata
{
    Task<ArtistMetadata?> GetMetadataAsync(
        string artistName,
        string? sourceArtistId,
        CancellationToken cancellationToken);
}
