using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.GetArtist.Contract;

namespace Soundtrail.Services.Api.Features.GetArtist.Adapters;

public interface IGetArtistPort
{
    Task<GetArtistResponse?> GetArtistAsync(ArtistId artistId, CancellationToken cancellationToken);
}
