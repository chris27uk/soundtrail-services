using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.MusicBrainzDumpFreshness;

namespace Soundtrail.Adapters.MusicBrainzDumpFreshness;

public interface IMusicBrainzDumpFreshnessEvaluator
{
    Task<MusicBrainzDumpFreshnessDecision> EvaluateArtistAlbumsAsync(
        ArtistId artistId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);

    Task<MusicBrainzDumpFreshnessDecision> EvaluateArtistTracksAsync(
        ArtistId artistId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);

    Task<MusicBrainzDumpFreshnessDecision> EvaluateAlbumTracksAsync(
        AlbumId albumId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);
}
