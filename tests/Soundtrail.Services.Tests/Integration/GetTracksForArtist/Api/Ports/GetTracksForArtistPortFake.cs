using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;

namespace Soundtrail.Services.Tests.Integration.GetTracksForArtist.Api.Ports;

internal sealed class GetTracksForArtistPortFake : IGetTracksForArtistPort
{
    private readonly Func<ArtistId, CancellationToken, Task<GetTracksForArtistResponse?>>? resolver;
    private GetTracksForArtistResponse? response;

    public GetTracksForArtistPortFake(GetTracksForArtistResponse? response = null) => this.response = response;

    private GetTracksForArtistPortFake(
        Func<ArtistId, CancellationToken, Task<GetTracksForArtistResponse?>> resolver) =>
        this.resolver = resolver;

    public List<ArtistId> RequestedArtistIds { get; } = [];

    public static GetTracksForArtistPortFake Create(
        Func<ArtistId, CancellationToken, Task<GetTracksForArtistResponse?>> resolver) =>
        new(resolver);

    public void Seed(GetTracksForArtistResponse? tracks) => response = tracks;

    public Task<GetTracksForArtistResponse?> GetTracksForArtistAsync(ArtistId artistId, CancellationToken cancellationToken)
    {
        RequestedArtistIds.Add(artistId);
        return resolver is null
            ? Task.FromResult(response)
            : resolver(artistId, cancellationToken);
    }
}
