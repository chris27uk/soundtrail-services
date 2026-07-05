using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetAlbum.Contract;

namespace Soundtrail.Services.Api.Features.GetAlbum.Adapters
{
    public interface IGetAlbumPort
    {
        Task<GetAlbumResponse?> GetAlbumAsync(AlbumId albumId, CancellationToken cancellationToken);
    }
}
