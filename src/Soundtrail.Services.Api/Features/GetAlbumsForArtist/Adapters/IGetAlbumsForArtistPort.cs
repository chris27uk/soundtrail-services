using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Api.Features.GetAlbumsForArtist.Adapters;

public interface IGetAlbumsForArtistPort
{
    Task<GetAlbumsForArtistResponse?> GetAlbumsForArtistAsync(ArtistId artistId, CancellationToken cancellationToken);
}
