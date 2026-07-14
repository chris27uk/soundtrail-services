using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.GetArtist.Adapters;
using Soundtrail.Services.Api.Features.GetArtist.Contract;

namespace Soundtrail.Services.Tests.Integration.Ports.GetArtist;

internal sealed class GetArtistPortFake(GetArtistResponse? response = null) : IGetArtistPort
{
    public Task<GetArtistResponse?> GetArtistAsync(ArtistId artistId, CancellationToken cancellationToken) => Task.FromResult(response);
}
