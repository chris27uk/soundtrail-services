using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Operations;
using Soundtrail.Services.Internal.Projector.Features.OnLookupRecorded;

namespace Soundtrail.Services.Tests.Unit.Projector.OnLookupRecorded;

internal sealed class DiscoveryOutcomeProjectorUnitTestEnvironment
{
    private DiscoveryOutcomeProjectorUnitTestEnvironment(
        CommandBusFake commandBus,
        ArtistCatalogRepositoryFake repository)
    {
        CommandBus = commandBus;
        Repository = repository;
    }

    public CommandBusFake CommandBus { get; }

    public ArtistCatalogRepositoryFake Repository { get; }

    public static DiscoveryOutcomeProjectorUnitTestEnvironment Create() =>
        new(new CommandBusFake(), new ArtistCatalogRepositoryFake());

    public DiscoveryOutcomeProjectorHandler CreateDiscoverySubject() => new(CommandBus, Repository);

    public StreamingLocationDiscoveredProjectorHandler CreateStreamingLocationSubject() => new(Repository);

    public static TrackDiscovered CreateTrackDiscovered() =>
        new(
            new Track(TestTrackIds.Create("projected-playlist-track"))
            {
                Title = "Road Song",
                ArtistName = "The Travellers",
                AlbumTitle = "Miles Ahead",
                ReleaseDate = new DateOnly(2020, 1, 1),
                ReleaseType = "studio"
            },
            new CatalogTrackHierarchy(ArtistId.From("artist-projector-1"), null),
            new DateTimeOffset(2026, 7, 19, 10, 20, 0, TimeSpan.Zero));

    public static PlaylistTracksDiscovered CreatePlaylistTracksDiscovered() =>
        new(
            Domain.Catalog.Playlists.PlaylistId.FromPlaylistName("Road Trip"),
            [TestTrackIds.Create("projected-playlist-track")],
            new DateTimeOffset(2026, 7, 19, 10, 21, 0, TimeSpan.Zero));

    public static StreamingLocationDiscovered CreateStreamingLocationDiscovered()
    {
        var trackId = TestTrackIds.Create("projected-streaming-track");
        var when = new DateTimeOffset(2026, 7, 19, 10, 25, 0, TimeSpan.Zero);

        return new StreamingLocationDiscovered(
            new CatalogItemId.Track(trackId),
            new CatalogTrackHierarchy(ArtistId.From("artist-projector-2"), null),
            ProviderName.Spotify,
            "spotify:track:456",
            new Uri("https://open.spotify.com/track/456"),
            LookupSource.Odesli,
            when);
    }

    public sealed class CommandBusFake : ICommandBus
    {
        public List<ICommand> Commands { get; } = [];

        public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return Task.CompletedTask;
        }
    }

    public sealed class ArtistCatalogRepositoryFake : IEventStreamRepository<ArtistId>
    {
        public IReadOnlyList<IDomainEvent> AppendedEvents { get; private set; } = [];

        public Task<LoadedEventStream<ArtistId>> LoadAsync(ArtistId streamId, CancellationToken cancellationToken) =>
            Task.FromResult(LoadedEventStream<ArtistId>.Empty(streamId));

        public Task<AppendResult> AppendAsync(
            LoadedEventStream<ArtistId> stream,
            IReadOnlyList<IDomainEvent> events,
            OperationId? operationId,
            CancellationToken cancellationToken)
        {
            AppendedEvents = events.ToArray();
            return Task.FromResult(new AppendResult(true, stream.Version + events.Count, events.ToArray(), AppendOutcome.Appended));
        }
    }
}
