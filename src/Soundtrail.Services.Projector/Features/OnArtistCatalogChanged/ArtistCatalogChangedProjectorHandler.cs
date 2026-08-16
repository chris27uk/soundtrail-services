using Soundtrail.Adapters.CatalogProjection;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged;

/// <summary>
/// Rebuilds artist-catalog read models from the artist event stream.
/// Invoked by <c>CatalogItemChangedProjectorHandler</c> only after the triggering
/// catalog-stream event has been appended, so Scrutor handler order cannot project
/// a stale stream (which left Midnight Signals without Spotify in CI).
/// </summary>
public sealed class ArtistCatalogChangedProjectorHandler(
    IEventStreamRepository<ArtistId> repository,
    IStoreArtistCatalogReadModelPort storeArtistCatalogReadModelPort,
    IStorePlaylistTracksReadModelPort storePlaylistTracksReadModelPort)
{
    public async Task Handle(ArtistId artistId, CancellationToken cancellationToken = default)
    {
        var stream = await repository.LoadAsync(artistId, cancellationToken);
        var projection = ArtistCatalogProjectionMaterializer.Build(artistId, stream.Events);
        await storeArtistCatalogReadModelPort.StoreAsync(projection, cancellationToken);

        foreach (var track in projection.Tracks)
        {
            await storePlaylistTracksReadModelPort.RepairTrackAsync(track.TrackId, cancellationToken);
        }
    }
}
