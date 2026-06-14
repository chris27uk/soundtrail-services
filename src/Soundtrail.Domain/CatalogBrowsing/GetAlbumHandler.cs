namespace Soundtrail.Domain.CatalogBrowsing;

public sealed class GetAlbumHandler(ICatalogReadPort catalogReadPort) : IHandler<GetAlbumCommand, AlbumDetailsResponse?>
{
    public Task<AlbumDetailsResponse?> Handle(GetAlbumCommand request, CancellationToken cancellationToken = default) =>
        catalogReadPort.GetAlbumAsync(request.ArtistId, request.AlbumId, cancellationToken);
}
