using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.GetArtist;
using Soundtrail.Services.Api.Features.GetArtist.Adapters;
using Soundtrail.Services.Api.Features.GetArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.GetArtist;

internal sealed class GetArtistUnitTestEnvironment
{
    private GetArtistUnitTestEnvironment(
        ArtistId artistId,
        GetArtistPortFake getArtistPortFake)
    {
        ArtistId = artistId;
        Port = getArtistPortFake;
    }

    public ArtistId ArtistId { get; }

    public GetArtistPortFake Port { get; }

    public static GetArtistUnitTestEnvironment ForExistingArtist(
        ArtistId? artistId = null,
        GetArtistResponse? response = null) =>
        new(
            artistId ?? ArtistExistsTestData.DefaultArtistId,
            new GetArtistPortFake(response ?? ArtistExistsTestData.CreateResponse(artistId: artistId ?? ArtistExistsTestData.DefaultArtistId)));

    public GetArtistHandler CreateSubjectUnderTest() => new(Port);

    public GetArtistRequest CreateRequest() => new(ArtistId);

    public sealed class GetArtistPortFake(GetArtistResponse? response) : IGetArtistPort
    {
        public List<ArtistId> RequestedArtistIds { get; } = [];

        public Task<GetArtistResponse?> GetArtistAsync(ArtistId artistId, CancellationToken cancellationToken)
        {
            RequestedArtistIds.Add(artistId);
            return Task.FromResult(response);
        }
    }
}
