using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetArtist.Adapters;

public interface IGetArtistPort
{
    Task<GetArtistResponse?> GetArtistAsync(ArtistId artistId, CancellationToken cancellationToken);
}
