using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged;

public sealed class CatalogSearchCandidateChangedProjectorHandler(IStoreCatalogSearchCandidatePort storeCatalogSearchCandidatePort)
{
    public Task Handle(ArtistDiscovered @event, CancellationToken cancellationToken = default) =>
        StoreAsync(
            new CatalogSearchCandidateProjection(
                @event.Artist.Id.Value,
                "artist",
                @event.Artist.Name.Value,
                @event.Artist.Name.Value,
                null,
                null,
                @event.Artist.ImageUrl,
                @event.ObservedAt),
            cancellationToken);

    public Task Handle(AlbumDiscovered @event, CancellationToken cancellationToken = default) =>
        StoreAsync(
            new CatalogSearchCandidateProjection(
                @event.Album.AlbumId.StableValue,
                "album",
                @event.Album.AlbumTitle ?? string.Empty,
                @event.Album.AlbumTitle ?? string.Empty,
                null,
                @event.Album.AlbumTitle,
                @event.Album.ArtworkUrl,
                @event.ObservedAt),
            cancellationToken);

    public Task Handle(TrackDiscovered @event, CancellationToken cancellationToken = default) =>
        StoreAsync(
            new CatalogSearchCandidateProjection(
                @event.Track.TrackId.Value,
                "track",
                $"{@event.Track.Title} {@event.Track.ArtistName}".Trim(),
                @event.Track.Title,
                @event.Track.ArtistName,
                @event.Track.AlbumTitle,
                @event.Track.ArtworkUrl,
                @event.ObservedAt),
            cancellationToken);

    private Task StoreAsync(CatalogSearchCandidateProjection projection, CancellationToken cancellationToken) =>
        storeCatalogSearchCandidatePort.StoreAsync(projection, cancellationToken);
}
