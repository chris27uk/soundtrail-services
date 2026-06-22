using Soundtrail.Domain;
using Soundtrail.Domain.CatalogBrowsing;

namespace Soundtrail.Services.Api.Features.GetAlbum;

public sealed class GetAlbumHandler(ICatalogReadPort catalogReadPort) : IApiHandler<GetAlbumCommand, AlbumDetailsResponse?>
{
    public Task<AlbumDetailsResponse?> Handle(GetAlbumCommand request, CancellationToken cancellationToken = default) => catalogReadPort.GetAlbumAsync(request.ArtistId, request.AlbumId, cancellationToken);
}
