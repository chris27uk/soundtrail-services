using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogTrackChanged;

public sealed class CatalogTrackChangedProjectorHandler(
    IStorePlaylistTracksReadModelPort storePlaylistTracksReadModelPort)
{
    public Task Handle(TrackId trackId, CancellationToken cancellationToken = default) =>
        storePlaylistTracksReadModelPort.RepairTrackAsync(trackId, cancellationToken);
}
