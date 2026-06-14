namespace Soundtrail.Domain.CatalogBrowsing;

public sealed class ListTracksByAlbumHandler(ICatalogReadPort catalogReadPort) : IHandler<ListTracksByAlbumCommand, AlbumTracksResponse?>
{
    public Task<AlbumTracksResponse?> Handle(ListTracksByAlbumCommand request, CancellationToken cancellationToken = default) =>
        catalogReadPort.ListTracksByAlbumAsync(request.ArtistId, request.AlbumId, cancellationToken);
}
