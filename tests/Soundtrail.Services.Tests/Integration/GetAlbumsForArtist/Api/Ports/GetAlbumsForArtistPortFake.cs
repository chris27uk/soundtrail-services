using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Tests.Integration.GetAlbumsForArtist.Api.Ports;

internal sealed class GetAlbumsForArtistPortFake : IGetAlbumsForArtistPort
{
    private readonly Func<ArtistId, CancellationToken, Task<GetAlbumsForArtistResponse?>>? resolver;
    private GetAlbumsForArtistResponse? response;

    public GetAlbumsForArtistPortFake(GetAlbumsForArtistResponse? response = null) => this.response = response;

    private GetAlbumsForArtistPortFake(
        Func<ArtistId, CancellationToken, Task<GetAlbumsForArtistResponse?>> resolver) =>
        this.resolver = resolver;

    public List<ArtistId> RequestedArtistIds { get; } = [];

    public static GetAlbumsForArtistPortFake Create(
        Func<ArtistId, CancellationToken, Task<GetAlbumsForArtistResponse?>> resolver) =>
        new(resolver);

    public void Seed(GetAlbumsForArtistResponse? albums) => response = albums;

    public Task<GetAlbumsForArtistResponse?> GetAlbumsForArtistAsync(ArtistId artistId, CancellationToken cancellationToken)
    {
        RequestedArtistIds.Add(artistId);
        return resolver is null
            ? Task.FromResult(response)
            : resolver(artistId, cancellationToken);
    }
}
