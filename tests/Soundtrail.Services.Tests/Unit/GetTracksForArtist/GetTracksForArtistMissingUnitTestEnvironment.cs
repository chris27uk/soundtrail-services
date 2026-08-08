using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;
using Soundtrail.Services.Tests.Fakes;

namespace Soundtrail.Services.Tests.Unit.GetTracksForArtist;

internal sealed class GetTracksForArtistMissingUnitTestEnvironment
{
    private GetTracksForArtistMissingUnitTestEnvironment(
        ArtistId artistId,
        GetTracksForArtistPortFake port,
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

    public GetTracksForArtistPortFake Port { get; }

    public CommandBusFake CommandBus { get; }

    public DiscoveryFeedbackPortFake DiscoveryFeedbackPort { get; }

    public ClockFake Clock { get; }

    public static GetTracksForArtistMissingUnitTestEnvironment ForMissingArtistTracks(ArtistId? artistId = null) =>
        new(
            artistId ?? ArtistId.From("artist-2307"),
            new GetTracksForArtistPortFake(),
            new CommandBusFake(),
            new DiscoveryFeedbackPortFake(),
            new ClockFake(new DateTimeOffset(2024, 6, 7, 8, 9, 10, TimeSpan.Zero)));

    public GetTracksForArtistHandler CreateSubjectUnderTest() => new(Port, CommandBus, DiscoveryFeedbackPort, Clock);

    public GetTracksForArtistRequest CreateRequest() => new(ArtistId);

    public sealed class GetTracksForArtistPortFake : IGetTracksForArtistPort
    {
        public List<ArtistId> RequestedArtistIds { get; } = [];

        public Task<GetTracksForArtistResponse?> GetTracksForArtistAsync(ArtistId artistId, CancellationToken cancellationToken)
        {
            RequestedArtistIds.Add(artistId);
            return Task.FromResult<GetTracksForArtistResponse?>(null);
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
