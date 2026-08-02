using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Operations;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;

namespace Soundtrail.Services.Tests.Unit.Projector.OnArtistCatalogChanged;

public sealed class ArtistCatalogChangedProjectsStreamingLocationsTests
{
    [Fact]
    public async Task Given_A_Streaming_Location_Was_Discovered_When_Projecting_Then_The_Track_Is_Playable()
    {
        var artistId = ArtistId.From("musicbrainz-artist:test-artist");
        var trackId = TestTrackIds.Create("artist-catalog-streaming-track");
        var storePort = new StoreArtistCatalogReadModelPortFake();
        var repository = new ArtistCatalogRepositoryFake(
        [
            new TrackDiscovered(
                new Track(trackId)
                {
                    Title = "Midnight Signals",
                    ArtistName = "Aurora Lane"
                },
                new CatalogTrackHierarchy(artistId, null),
                new DateTimeOffset(2026, 8, 1, 13, 0, 0, TimeSpan.Zero)),
            new StreamingLocationDiscovered(
                new CatalogItemId.Track(trackId),
                new CatalogTrackHierarchy(artistId, null),
                ProviderName.Spotify,
                "spotify-track-123",
                new Uri("https://open.spotify.com/track/123"),
                LookupSource.Odesli,
                new DateTimeOffset(2026, 8, 1, 13, 1, 0, TimeSpan.Zero))
        ]);
        var subject = new ArtistCatalogChangedProjectorHandler(repository, storePort);

        await subject.Handle(artistId);

        var track = storePort.StoredReadModel!.Tracks.Should().ContainSingle().Subject;
        track.StreamingLocations.Should().ContainSingle().Which.Should().Be(
            new ArtistCatalogStreamingLocationReadModel(
                ProviderName.Spotify,
                "spotify-track-123",
                "https://open.spotify.com/track/123"));
    }

    private sealed class ArtistCatalogRepositoryFake(IReadOnlyList<IDomainEvent> events) : IEventStreamRepository<ArtistId>
    {
        public Task<LoadedEventStream<ArtistId>> LoadAsync(ArtistId streamId, CancellationToken cancellationToken) =>
            Task.FromResult(new LoadedEventStream<ArtistId>(streamId, events.Count, events));

        public Task<AppendResult> AppendAsync(
            LoadedEventStream<ArtistId> stream,
            IReadOnlyList<IDomainEvent> eventsToAppend,
            OperationId? operationId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new AppendResult(false, stream.Version, [], AppendOutcome.DuplicateOperation));
    }

    private sealed class StoreArtistCatalogReadModelPortFake : IStoreArtistCatalogReadModelPort
    {
        public ArtistCatalogReadModel? StoredReadModel { get; private set; }

        public Task StoreAsync(ArtistCatalogReadModel readModel, CancellationToken cancellationToken)
        {
            StoredReadModel = readModel;
            return Task.CompletedTask;
        }
    }
}
