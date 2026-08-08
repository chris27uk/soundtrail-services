using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Contract;

namespace Soundtrail.Services.Tests.Integration.GetArtist.Api.Ports;

internal sealed class GetArtistPortFake : IGetArtistPort
{
    private GetArtistResponse? response;

    public GetArtistPortFake(GetArtistResponse? response = null) => this.response = response;

    public List<ArtistId> RequestedArtistIds { get; } = [];

    public void Seed(GetArtistResponse? artist) => response = artist;

    public Task<GetArtistResponse?> GetArtistAsync(ArtistId artistId, CancellationToken cancellationToken)
    {
        RequestedArtistIds.Add(artistId);
        return Task.FromResult(response);
    }
}
