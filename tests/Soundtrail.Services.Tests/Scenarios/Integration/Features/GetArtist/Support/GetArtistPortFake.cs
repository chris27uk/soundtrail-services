using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Contract;

namespace Soundtrail.Services.Tests.Integration.Features.GetArtist.Support;

internal sealed class GetArtistPortFake : IGetArtistPort
{
    private GetArtistResponse? response;

    public GetArtistPortFake(GetArtistResponse? response = null) => this.response = response;

    public List<ArtistId> RequestedArtistIds { get; } = [];

    public void Seed(GetArtistResponse? artist) => this.response = artist;

    public Task<GetArtistResponse?> GetArtistAsync(ArtistId artistId, CancellationToken cancellationToken)
    {
        RequestedArtistIds.Add(artistId);
        return Task.FromResult(this.response);
    }
}
