using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;
using Soundtrail.Services.Tests.Fakes;

namespace Soundtrail.Services.Tests.Unit.GetAlbumsForArtist;

internal sealed class GetAlbumsForArtistMissingUnitTestEnvironment
{
    private GetAlbumsForArtistMissingUnitTestEnvironment(
        ArtistId artistId,
        GetAlbumsForArtistPortFake port,
        CommandBusFake commandBus,
        DiscoveryFeedbackPortFake discoveryFeedbackPort,
        ClockFake clock)
    {
        ArtistId = artistId;
        Port = port;
        CommandBus = commandBus;
        DiscoveryFeedbackPort = discoveryFeedbackPort;
        Clock = clock;
    }

    public ArtistId ArtistId { get; }

    public GetAlbumsForArtistPortFake Port { get; }

    public CommandBusFake CommandBus { get; }

    public DiscoveryFeedbackPortFake DiscoveryFeedbackPort { get; }

    public ClockFake Clock { get; }

    public static GetAlbumsForArtistMissingUnitTestEnvironment ForMissingArtistAlbums(ArtistId? artistId = null) =>
        new(
            artistId ?? ArtistId.From("artist-1707"),
            new GetAlbumsForArtistPortFake(),
            new CommandBusFake(),
            new DiscoveryFeedbackPortFake(),
            new ClockFake(new DateTimeOffset(2024, 6, 7, 8, 9, 10, TimeSpan.Zero)));

    public GetAlbumsForArtistHandler CreateSubjectUnderTest() => new(Port, CommandBus, DiscoveryFeedbackPort, Clock);

    public GetAlbumsForArtistRequest CreateRequest() => new(ArtistId);

    public sealed class GetAlbumsForArtistPortFake : IGetAlbumsForArtistPort
    {
        public List<ArtistId> RequestedArtistIds { get; } = [];

        public Task<GetAlbumsForArtistResponse?> GetAlbumsForArtistAsync(ArtistId artistId, CancellationToken cancellationToken)
        {
            RequestedArtistIds.Add(artistId);
            return Task.FromResult<GetAlbumsForArtistResponse?>(null);
        }
    }

    public sealed class DiscoveryFeedbackPortFake : IDiscoveryFeedbackPort
    {
        public EnrichmentTarget? RequestedTarget { get; private set; }

        public DiscoveryFeedbackResponse? Response { get; set; }

        public Task<DiscoveryFeedbackResponse?> GetAsync(EnrichmentTarget target, CancellationToken cancellationToken)
        {
            RequestedTarget = target;
            return Task.FromResult(Response);
        }
    }
}
