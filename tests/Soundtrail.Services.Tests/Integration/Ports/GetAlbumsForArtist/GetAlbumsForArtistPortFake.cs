using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Tests.Integration.Ports.GetAlbumsForArtist;

internal sealed class GetAlbumsForArtistPortFake(GetAlbumsForArtistResponse? response = null) : IGetAlbumsForArtistPort
{
    public Task<GetAlbumsForArtistResponse?> GetAlbumsForArtistAsync(ArtistId artistId, CancellationToken cancellationToken) => Task.FromResult(response);
}
