using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetAlbum.Adapters
{
    public interface IGetAlbumPort
    {
        Task<GetAlbumResponse?> GetAlbumAsync(AlbumId albumId, CancellationToken cancellationToken);
    }
}
