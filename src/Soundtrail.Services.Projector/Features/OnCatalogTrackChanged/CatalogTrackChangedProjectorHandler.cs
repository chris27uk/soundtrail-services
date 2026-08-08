using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogTrackChanged;

/// <summary>
/// Playlist repair after catalog-track changes is owned by
/// <c>ArtistCatalogChangedProjectorHandler</c> (invoked only after the artist stream
/// append). This type remains for solitary tests that exercise repair in isolation.
/// </summary>
public sealed class CatalogTrackChangedProjectorHandler(
    IStorePlaylistTracksReadModelPort storePlaylistTracksReadModelPort)
{
    public Task Handle(TrackId trackId, CancellationToken cancellationToken = default) =>
        storePlaylistTracksReadModelPort.RepairTrackAsync(trackId, cancellationToken);
}
