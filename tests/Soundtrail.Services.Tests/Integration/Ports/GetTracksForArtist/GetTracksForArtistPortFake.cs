using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;

namespace Soundtrail.Services.Tests.Integration.Ports.GetTracksForArtist;

internal sealed class GetTracksForArtistPortFake(GetTracksForArtistResponse? response = null) : IGetTracksForArtistPort
{
    public Task<GetTracksForArtistResponse?> GetTracksForArtistAsync(ArtistId artistId, CancellationToken cancellationToken) => Task.FromResult(response);
}
