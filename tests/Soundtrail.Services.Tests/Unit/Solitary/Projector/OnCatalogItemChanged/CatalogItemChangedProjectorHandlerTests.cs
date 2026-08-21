using Soundtrail.Adapters.CatalogProjection;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogItemChanged;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Tests.Unit.Solitary.Projector.OnCatalogItemChanged;

public sealed class CatalogItemChangedProjectorHandlerTests
{
    [Fact]
    public async Task Given_Streaming_Location_When_Handling_Then_Artist_Catalog_Is_Projected_After_Stream_Append()
    {
        var environment = TestEnvironment.Create();
        var observedAt = DateTimeOffset.Parse("2024-01-15T12:00:00Z");
        var track = new Track(environment.TrackId)
        {
            Title = "Midnight Signals",
            ArtistName = "Aurora Lane",
            UpdatedAt = observedAt
        };

        await environment.CatalogItemChanged.Handle(
            new TrackDiscovered(
                track,
                new CatalogTrackHierarchy(environment.ArtistId, AlbumId: null),
                observedAt),
            CancellationToken.None);

        environment.ArtistCatalogPort.StoreCount.Should().Be(1);
        environment.ArtistCatalogPort.LastStored!.Tracks.Should().ContainSingle()
            .Which.StreamingLocations.Should().BeEmpty();

        await environment.CatalogItemChanged.Handle(
            new StreamingLocationDiscovered(
                new CatalogItemId.Track(environment.TrackId),
                new CatalogTrackHierarchy(environment.ArtistId, AlbumId: null),
                ProviderName.Spotify,
                "midnight-signals",
                new Uri("https://open.spotify.com/track/midnight-signals"),
                LookupSource.Odesli,
                observedAt.AddMinutes(1)),
            CancellationToken.None);

        environment.ArtistCatalogPort.StoreCount.Should().Be(2);
        environment.ArtistCatalogPort.LastStored!.Tracks.Should().ContainSingle()
            .Which.StreamingLocations.Should().ContainSingle()
            .Which.Url.Should().Be("https://open.spotify.com/track/midnight-signals");
        environment.PlaylistPort.RepairedTrackIds.Should().Contain(environment.TrackId);
    }

    private sealed class TestEnvironment
    {
        public ArtistId ArtistId { get; } = ArtistId.From("aurora-lane");

        public TrackId TrackId { get; } =
            TrackId.TryCreate("Aurora Lane", "Midnight Signals") switch
            {
                TrackIdCreateResult.Success success => success.Value,
                TrackIdCreateResult.Failure failure => throw new InvalidOperationException(failure.Reason),
                _ => throw new InvalidOperationException("Unexpected TrackId creation result.")
            };

        public EventStreamRepositoryFake Repository { get; } = new();

        public StoreArtistCatalogReadModelPortFake ArtistCatalogPort { get; } = new();

        public StorePlaylistTracksReadModelPortFake PlaylistPort { get; } = new();

        public CatalogItemChangedProjectorHandler CatalogItemChanged { get; }

        private TestEnvironment()
        {
            this.Repository.StreamId = this.ArtistId;
            var artistCatalogChanged = new ArtistCatalogChangedProjectorHandler(
                this.Repository,
                this.ArtistCatalogPort,
                this.PlaylistPort);
            this.CatalogItemChanged = new CatalogItemChangedProjectorHandler(
                this.Repository,
                artistCatalogChanged);
        }

        public static TestEnvironment Create() => new();
    }

    private sealed class EventStreamRepositoryFake : IEventStreamRepository<ArtistId>
    {
        private readonly List<IDomainEvent> events = [];

        public ArtistId StreamId { get; set; } = ArtistId.From("unused");

        public Task<LoadedEventStream<ArtistId>> LoadAsync(
            ArtistId streamId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new LoadedEventStream<ArtistId>(streamId, this.events.Count, this.events.ToArray()));

        public Task<AppendResult> AppendAsync(
            LoadedEventStream<ArtistId> stream,
            IReadOnlyList<IDomainEvent> newEvents,
            OperationId? operationId,
            CancellationToken cancellationToken,
            ProjectionHint? projectionHint = null,
            bool saveChanges = true)
        {
            _ = saveChanges;
            this.events.AddRange(newEvents);
            return Task.FromResult(new AppendResult(
                Appended: true,
                Version: this.events.Count,
                Events: newEvents,
                Outcome: AppendOutcome.Appended));
        }
    }

    private sealed class StoreArtistCatalogReadModelPortFake : IStoreArtistCatalogReadModelPort
    {
        public int StoreCount { get; private set; }

        public ArtistCatalogProjection? LastStored { get; private set; }

        public Task StoreAsync(ArtistCatalogProjection projection, CancellationToken cancellationToken)
        {
            this.StoreCount++;
            this.LastStored = projection;
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
