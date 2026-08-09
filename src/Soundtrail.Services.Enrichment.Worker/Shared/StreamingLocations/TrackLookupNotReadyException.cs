using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;

public sealed class TrackLookupNotReadyException(TrackId trackId) : Exception(
    $"Track '{trackId.Value}' is not ready for streaming lookup yet.")
{
    public TrackId TrackId { get; } = trackId;
}
