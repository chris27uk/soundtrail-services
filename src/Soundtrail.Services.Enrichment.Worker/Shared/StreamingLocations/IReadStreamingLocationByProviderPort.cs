using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;

public interface IReadStreamingLocationByProviderPort
{
    Task<Uri?> ReadByIsrcAsync(
        string isrc,
        ProviderName provider,
        CancellationToken cancellationToken);

    Task<Uri?> ReadByTrackMetadataAsync(
        string artistName,
        string trackTitle,
        ProviderName provider,
        CancellationToken cancellationToken);
}
