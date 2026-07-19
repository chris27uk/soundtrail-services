using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Adapters;

public interface IGetTracksForAlbumPort
{
    Task<GetTracksForAlbumResponse?> GetTracksForAlbumAsync(AlbumId albumId, CancellationToken cancellationToken);
}
