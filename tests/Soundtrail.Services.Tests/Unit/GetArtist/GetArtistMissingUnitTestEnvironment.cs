using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.GetArtist;
using Soundtrail.Services.Api.Features.GetArtist.Adapters;
using Soundtrail.Services.Api.Features.GetArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.GetArtist;

internal sealed class GetArtistMissingUnitTestEnvironment
{
    private GetArtistMissingUnitTestEnvironment(
        ArtistId artistId,
        GetArtistPortFake getArtistPortFake)
    {
        ArtistId = artistId;
        Port = getArtistPortFake;
    }

    public ArtistId ArtistId { get; }

    public GetArtistPortFake Port { get; }

    public static GetArtistMissingUnitTestEnvironment ForMissingArtist(ArtistId? artistId = null) =>
        new(
            artistId ?? ArtistId.From("artist-601"),
            new GetArtistPortFake());

    public GetArtistHandler CreateSubjectUnderTest() => new(Port);

    public GetArtistRequest CreateRequest() => new(ArtistId);

    public sealed class GetArtistPortFake : IGetArtistPort
    {
        public List<ArtistId> RequestedArtistIds { get; } = [];

        public Task<GetArtistResponse?> GetArtistAsync(ArtistId artistId, CancellationToken cancellationToken)
        {
            RequestedArtistIds.Add(artistId);
            return Task.FromResult<GetArtistResponse?>(null);
        }
    }
}
