using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;

public interface IReadTrackForLookupPort
{
    Task<TrackLookupContext?> ReadAsync(TrackId trackId, CancellationToken cancellationToken);
}

public sealed record TrackLookupContext(
    ArtistId ArtistId,
    TrackId TrackId,
    string Title,
    string ArtistName,
    string? Isrc);
