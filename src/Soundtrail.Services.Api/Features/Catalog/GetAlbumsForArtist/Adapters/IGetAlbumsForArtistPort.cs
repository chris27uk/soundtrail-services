using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Adapters;

public interface IGetAlbumsForArtistPort
{
    Task<GetAlbumsForArtistResponse?> GetAlbumsForArtistAsync(ArtistId artistId, CancellationToken cancellationToken);
}
