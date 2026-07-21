using Soundtrail.Domain.Abstractions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady;

namespace Soundtrail.Services.Tests.Unit.Orchestrator.OnLookupWorkReady;

internal sealed class LookupWorkReadyHandlerUnitTestEnvironment
{
    private LookupWorkReadyHandlerUnitTestEnvironment(CommandBusFake commandBus)
    {
        CommandBus = commandBus;
    }

    public CommandBusFake CommandBus { get; }

    public static LookupWorkReadyHandlerUnitTestEnvironment Create() => new(new CommandBusFake());

    public LookupWorkReadyHandler CreateSubject() => new(CommandBus);

    public static DispatchLookupWork CreateSearchRequest() =>
        new(
            new EnrichmentTarget.SearchForUnknownCatalogItem(new SearchCriteria("u2", SearchType.Artist)),
            LookupPriorityBand.High,
            MessageId.For("cmd-search"),
            CorrelationId.From("corr-search"),
            new DateTimeOffset(2026, 7, 18, 9, 10, 0, TimeSpan.Zero));

    public static DispatchLookupWork CreateStreamingLocationRequest(string commandId = "cmd-streaming") =>
        new(
            Work.EnrichTrackStreamingLocation(TestTrackIds.Create("track-2901")),
            LookupPriorityBand.Low,
            MessageId.For(commandId),
            CorrelationId.From($"corr:{commandId}"),
            new DateTimeOffset(2026, 7, 18, 9, 11, 0, TimeSpan.Zero));

    public static DispatchLookupWork CreatePlaylistRequest() =>
        new(
            Work.DiscoverPlaylistTracks(PlaylistId.FromPlaylistName("roadtrip")),
            LookupPriorityBand.Low,
            MessageId.For("cmd-playlist"),
            CorrelationId.From("corr-playlist"),
            new DateTimeOffset(2026, 7, 18, 9, 12, 0, TimeSpan.Zero));

    public sealed class CommandBusFake : ICommandBus
    {
        public List<IMessage> Commands { get; } = [];

        public Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            Commands.Add(message);
            return Task.CompletedTask;
        }
    }
}
