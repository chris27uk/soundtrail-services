using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogTrackChanged;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Operations;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogItemChanged;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered;

namespace Soundtrail.Services.Tests.Unit.Projector.OnCatalogItemChanged;

internal sealed class CatalogItemChangedProjectorUnitTestEnvironment
{
    private CatalogItemChangedProjectorUnitTestEnvironment(
        CommandBusFake commandBus,
        ArtistCatalogRepositoryFake repository,
        StorePlaylistTracksReadModelPortFake storePlaylistTracksReadModelPort)
    {
        CommandBus = commandBus;
        Repository = repository;
        StorePlaylistTracksReadModelPort = storePlaylistTracksReadModelPort;
    }

    public CommandBusFake CommandBus { get; }

    public ArtistCatalogRepositoryFake Repository { get; }

    public StorePlaylistTracksReadModelPortFake StorePlaylistTracksReadModelPort { get; }

    public static CatalogItemChangedProjectorUnitTestEnvironment Create() =>
        new(new CommandBusFake(), new ArtistCatalogRepositoryFake(), new StorePlaylistTracksReadModelPortFake());

    public CatalogItemChangedProjectorHandler CreateCatalogItemSubject() => new(Repository);

    public PlaylistTracksDiscoveredProjectorHandler CreatePlaylistSubject() => new(CommandBus, StorePlaylistTracksReadModelPort);

    public CatalogTrackChangedProjectorHandler CreateCatalogTrackChangedSubject() => new(StorePlaylistTracksReadModelPort);

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
        public List<IMessage> Commands { get; } = [];

        public Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            Commands.Add(message);
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

    public sealed class StorePlaylistTracksReadModelPortFake : IStorePlaylistTracksReadModelPort
    {
        public PlaylistTracksDiscovered? StoredEvent { get; private set; }

        public TrackId? RepairedTrackId { get; private set; }

        public Task StoreAsync(PlaylistTracksDiscovered @event, CancellationToken cancellationToken)
        {
            StoredEvent = @event;
            return Task.CompletedTask;
        }

        public Task RepairTrackAsync(TrackId trackId, CancellationToken cancellationToken)
        {
            RepairedTrackId = trackId;
            return Task.CompletedTask;
        }
    }
}
