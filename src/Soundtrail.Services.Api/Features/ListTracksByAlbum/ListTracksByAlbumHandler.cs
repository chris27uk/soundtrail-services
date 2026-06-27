using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Services.Api.Infrastructure.Ports;

namespace Soundtrail.Services.Api.Features.ListTracksByAlbum;

public sealed class ListTracksByAlbumHandler(ICatalogReadPort catalogReadPort) : IApiHandler<ListTracksByAlbumCommand, AlbumTracksResponse?>
{
    public Task<AlbumTracksResponse?> Handle(ListTracksByAlbumCommand request, CancellationToken cancellationToken = default) => catalogReadPort.ListTracksByAlbumAsync(request.ArtistId, request.AlbumId, cancellationToken);
}
