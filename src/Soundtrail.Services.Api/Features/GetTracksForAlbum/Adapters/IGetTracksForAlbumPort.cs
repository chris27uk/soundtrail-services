using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Services.Api.Features.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Api.Features.GetTracksForAlbum.Adapters;

public interface IGetTracksForAlbumPort
{
    Task<GetTracksForAlbumResponse?> GetTracksForAlbumAsync(AlbumId albumId, CancellationToken cancellationToken);
}
