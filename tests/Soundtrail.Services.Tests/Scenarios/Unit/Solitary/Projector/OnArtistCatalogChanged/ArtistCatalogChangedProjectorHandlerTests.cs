using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;

namespace Soundtrail.Services.Tests.Unit.Solitary.Projector.OnArtistCatalogChanged;

public sealed class ArtistCatalogChangedProjectorHandlerTests
{
    [Fact]
    public async Task Given_Streaming_Location_Arrives_Before_Track_When_Projecting_Then_Provider_References_Survive_Track_Discovered()
    {
        var environment = TestEnvironment.Create();
        var observedAt = DateTimeOffset.Parse("2024-06-23T12:00:00Z");

        environment.Repository.Events =
        [
            new StreamingLocationDiscovered(
                new CatalogItemId.Track(environment.TrackId),
                new CatalogTrackHierarchy(environment.ArtistId, AlbumId: null),
                ProviderName.YoutubeMusic,
                "glass-cities-radio",
                new Uri("https://music.youtube.com/watch?v=glass-cities-radio"),
                LookupSource.Odesli,
                observedAt),
            new TrackDiscovered(
                new Track(environment.TrackId)
                {
                    Title = "Glass Cities (Radio Edit)",
                    ArtistName = "Neon Harbour",
                    AlbumTitle = "Glass Cities Remixes",
                    DurationMs = 180000,
                    ReleaseDate = new DateOnly(2024, 6, 23),
                    ReleaseType = "Radio Edit",
                    UpdatedAt = observedAt.AddMinutes(1)
                },
                new CatalogTrackHierarchy(environment.ArtistId, AlbumId: null),
                observedAt.AddMinutes(1))
        ];

        await environment.Subject.Handle(environment.ArtistId, CancellationToken.None);

        var stored = environment.ArtistCatalogPort.LastStored;
        stored.Should().NotBeNull();
        var track = stored!.Tracks.Should().ContainSingle().Subject;
        track.Title.Should().Be("Glass Cities (Radio Edit)");
        track.StreamingLocations.Should().ContainSingle()
            .Which.Url.Should().Be("https://music.youtube.com/watch?v=glass-cities-radio");
    }

    [Fact]
    public async Task Given_Artist_Catalog_Projected_When_Handling_Then_Repairs_Playlist_For_Each_Track()
    {
        var environment = TestEnvironment.Create();
        var observedAt = DateTimeOffset.Parse("2024-06-23T12:00:00Z");

        environment.Repository.Events =
        [
            new TrackDiscovered(
                new Track(environment.TrackId)
                {
                    Title = "Glass Cities (Radio Edit)",
                    ArtistName = "Neon Harbour",
                    UpdatedAt = observedAt
                },
                new CatalogTrackHierarchy(environment.ArtistId, AlbumId: null),
                observedAt),
            new StreamingLocationDiscovered(
                new CatalogItemId.Track(environment.TrackId),
                new CatalogTrackHierarchy(environment.ArtistId, AlbumId: null),
                ProviderName.YoutubeMusic,
                "glass-cities-radio",
                new Uri("https://music.youtube.com/watch?v=glass-cities-radio"),
                LookupSource.Odesli,
                observedAt.AddMinutes(1))
        ];

        await environment.Subject.Handle(environment.ArtistId, CancellationToken.None);

        environment.PlaylistPort.RepairedTrackIds.Should().Equal(environment.TrackId);
        environment.ArtistCatalogPort.LastStored.Should().NotBeNull();
    }

    private sealed class TestEnvironment
    {
        public ArtistId ArtistId { get; } = ArtistId.From("neon-harbour");

        public TrackId TrackId { get; } =
            TrackId.TryCreate("Neon Harbour", "Glass Cities (Radio Edit)") switch
            {
                TrackIdCreateResult.Success success => success.Value,
                TrackIdCreateResult.Failure failure => throw new InvalidOperationException(failure.Reason),
                _ => throw new InvalidOperationException("Unexpected TrackId creation result.")
            };

        public EventStreamRepositoryFake Repository { get; } = new();

        public StoreArtistCatalogReadModelPortFake ArtistCatalogPort { get; } = new();

        public StorePlaylistTracksReadModelPortFake PlaylistPort { get; } = new();

        public ArtistCatalogChangedProjectorHandler Subject { get; }

        private TestEnvironment()
        {
            this.Repository.StreamId = this.ArtistId;
            this.Subject = new ArtistCatalogChangedProjectorHandler(
                this.Repository,
                this.ArtistCatalogPort,
                this.PlaylistPort);
        }

        public static TestEnvironment Create() => new();
    }

    private sealed class EventStreamRepositoryFake : IEventStreamRepository<ArtistId>
    {
        public ArtistId StreamId { get; set; } = ArtistId.From("unused");

        public IReadOnlyList<IDomainEvent> Events { get; set; } = [];

        public Task<LoadedEventStream<ArtistId>> LoadAsync(
            ArtistId streamId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new LoadedEventStream<ArtistId>(streamId, this.Events.Count, this.Events));

        public Task<AppendResult> AppendAsync(
            LoadedEventStream<ArtistId> stream,
            IReadOnlyList<IDomainEvent> events,
            OperationId? operationId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StoreArtistCatalogReadModelPortFake : IStoreArtistCatalogReadModelPort
    {
        public ArtistCatalogReadModel? LastStored { get; private set; }

        public Task StoreAsync(ArtistCatalogReadModel readModel, CancellationToken cancellationToken)
        {
            this.LastStored = readModel;
            return Task.CompletedTask;
        }
    }

    private sealed class StorePlaylistTracksReadModelPortFake : IStorePlaylistTracksReadModelPort
    {
        public List<TrackId> RepairedTrackIds { get; } = [];

        public Task StoreAsync(PlaylistTracksDiscovered @event, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task RepairTrackAsync(TrackId trackId, CancellationToken cancellationToken)
        {
            this.RepairedTrackIds.Add(trackId);
            return Task.CompletedTask;
        }
    }
}
