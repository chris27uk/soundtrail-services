using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Tests.Unit.GetTracksForAlbum;

internal sealed class GetTracksForAlbumMissingUnitTestEnvironment
{
    private GetTracksForAlbumMissingUnitTestEnvironment(
        AlbumId albumId,
        GetTracksForAlbumPortFake getTracksForAlbumPortFake,
        CommandBusFake commandBus,
        DiscoveryFeedbackPortFake discoveryFeedbackPort,
        ClockPortFake clock)
    {
        AlbumId = albumId;
        Port = getTracksForAlbumPortFake;
        CommandBus = commandBus;
        DiscoveryFeedbackPort = discoveryFeedbackPort;
        Clock = clock;
    }

    public AlbumId AlbumId { get; }

    public GetTracksForAlbumPortFake Port { get; }

    public CommandBusFake CommandBus { get; }

    public DiscoveryFeedbackPortFake DiscoveryFeedbackPort { get; }

    public ClockPortFake Clock { get; }

    public static GetTracksForAlbumMissingUnitTestEnvironment ForMissingAlbumTracks(AlbumId? albumId = null) =>
        new(
            albumId ?? AlbumId.From("artist-1402", "album-1502"),
            new GetTracksForAlbumPortFake(),
            new CommandBusFake(),
            new DiscoveryFeedbackPortFake(),
            new ClockPortFake(new DateTimeOffset(2024, 6, 7, 8, 9, 10, TimeSpan.Zero)));

    public GetTracksForAlbumHandler CreateSubjectUnderTest() => new(Port, CommandBus, DiscoveryFeedbackPort, Clock);

    public GetTracksForAlbumRequest CreateRequest() => new(AlbumId);

    public sealed class GetTracksForAlbumPortFake : IGetTracksForAlbumPort
    {
        public List<AlbumId> RequestedAlbumIds { get; } = [];

        public Task<GetTracksForAlbumResponse?> GetTracksForAlbumAsync(AlbumId albumId, CancellationToken cancellationToken)
        {
            RequestedAlbumIds.Add(albumId);
            return Task.FromResult<GetTracksForAlbumResponse?>(null);
        }
    }

    public sealed class CommandBusFake : ICommandBus
    {
        public List<RequestKnownMusicDataMessage> Commands { get; } = [];

        public Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            Commands.Add((RequestKnownMusicDataMessage)message);
            return Task.CompletedTask;
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

    public sealed class ClockPortFake(DateTimeOffset utcNow) : IClockPort
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
