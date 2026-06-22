using Soundtrail.Domain;
using Soundtrail.Domain.CatalogBrowsing;

namespace Soundtrail.Services.Api.Features.ListTracksByAlbum;

public sealed class ListTracksByAlbumHandler(ICatalogReadPort catalogReadPort) : IApiHandler<ListTracksByAlbumCommand, AlbumTracksResponse?>
{
    public Task<AlbumTracksResponse?> Handle(ListTracksByAlbumCommand request, CancellationToken cancellationToken = default) => catalogReadPort.ListTracksByAlbumAsync(request.ArtistId, request.AlbumId, cancellationToken);
}
