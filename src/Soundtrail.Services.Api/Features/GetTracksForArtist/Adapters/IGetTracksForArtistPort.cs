using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetTracksForArtist.Contract;

namespace Soundtrail.Services.Api.Features.GetTracksForArtist.Adapters;

public interface IGetTracksForArtistPort
{
    Task<GetTracksForArtistResponse?> GetTracksForArtistAsync(ArtistId artistId, CancellationToken cancellationToken);
}
