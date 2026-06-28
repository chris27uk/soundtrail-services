using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.Adapters;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.EventStore;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.ProjectionReset;
using Soundtrail.Services.Tests.Support;

namespace Soundtrail.Services.Tests.Integration.MusicBrainzImport.Features.ReplayCatalogProjection;

internal sealed class ReplayCatalogProjectionTestEnvironment : IAsyncDisposable
{

    private ReplayCatalogProjectionTestEnvironment(
        ReplayCatalogProjectionHandler handler,
        Func<MusicCatalogId, Task<CatalogTrackRecordDto?>> loadTrackAsync,
        Func<MusicCatalogId, Task<int>> loadCheckpointVersionAsync,
        Func<Task<int>> countTrackDocumentsAsync,
        IAsyncDisposable? asyncDisposable = null)
    {
        Handler = handler;
        this.loadTrackAsync = loadTrackAsync;
        this.loadCheckpointVersionAsync = loadCheckpointVersionAsync;
        this.countTrackDocumentsAsync = countTrackDocumentsAsync;
        this.asyncDisposable = asyncDisposable;
    }

    private readonly Func<MusicCatalogId, Task<CatalogTrackRecordDto?>> loadTrackAsync;
    private readonly Func<MusicCatalogId, Task<int>> loadCheckpointVersionAsync;
    private readonly Func<Task<int>> countTrackDocumentsAsync;
    private readonly IAsyncDisposable? asyncDisposable;

    public ReplayCatalogProjectionHandler Handler { get; }

    public static async Task<ReplayCatalogProjectionTestEnvironment> CreateAsync(ReplayCatalogProjectionMode mode)
    {
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        var events = new[]
        {
            new VersionedMusicTrackEvent(
                1,
                new TrackDiscovered(
                    "Mr. Brightside",
                    "The Killers",
                    222000,
                    "USIR20400274",
                    "mbid-1",
                    LookupSource.MusicBrainz,
                    Clock)),
            new VersionedMusicTrackEvent(
                2,
                new ArtistDiscovered(
                    "artist_the_killers",
                    "The Killers",
                    "mb-artist-the-killers",
                    LookupSource.MusicBrainz,
                    Clock.AddMinutes(1))),
            new VersionedMusicTrackEvent(
                3,
                new AlbumDiscovered(
                    "album_hot_fuss",
                    "Hot Fuss",
                    "mb-release-hot-fuss",
                    new DateOnly(2004, 6, 7),
                    LookupSource.MusicBrainz,
                    Clock.AddMinutes(2)))
        };

        return mode switch
        {
            ReplayCatalogProjectionMode.InProcessFake => CreateFake(musicCatalogId, events),
            ReplayCatalogProjectionMode.RavenEmbedded => await CreateRavenAsync(musicCatalogId, events),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    public Task<CatalogTrackRecordDto?> LoadTrackAsync(MusicCatalogId musicCatalogId) =>
        loadTrackAsync(musicCatalogId);

    public Task<int> LoadCheckpointVersionAsync(MusicCatalogId musicCatalogId) =>
        loadCheckpointVersionAsync(musicCatalogId);

    public Task<int> CountTrackDocumentsAsync() =>
        countTrackDocumentsAsync();

    public async ValueTask DisposeAsync()
    {
        if (asyncDisposable is not null)
        {
            await asyncDisposable.DisposeAsync();
        }
    }

    private static ReplayCatalogProjectionTestEnvironment CreateFake(
        MusicCatalogId musicCatalogId,
        IReadOnlyList<VersionedMusicTrackEvent> events)
    {
        var eventStore = new FakeEventStore(new Dictionary<string, IReadOnlyList<VersionedMusicTrackEvent>>
        {
            [musicCatalogId.Value] = events
        });
        var projectionStore = new FakeProjectionStore();
        projectionStore.SeedStale(musicCatalogId);

        var handler = new ReplayCatalogProjectionHandler(
            eventStore,
            eventStore,
            projectionStore,
            new MusicCatalogChangedHandler(projectionStore, projectionStore));

        return new ReplayCatalogProjectionTestEnvironment(
            handler,
            id => Task.FromResult(projectionStore.LoadTrackDocument(id)),
            id => Task.FromResult(projectionStore.LoadCheckpointVersion(id)),
            () => Task.FromResult(projectionStore.TrackDocumentCount));
    }

    private static async Task<ReplayCatalogProjectionTestEnvironment> CreateRavenAsync(
        MusicCatalogId musicCatalogId,
        IReadOnlyList<VersionedMusicTrackEvent> events)
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        using var seedSession = raven.Store.OpenAsyncSession();
        var repository = TestEventStreamRepositories.CreateMusicTrack(seedSession);
        await repository.AppendAsync(
            LoadedEventStream<MusicCatalogId, IMusicTrackEvent>.Empty(musicCatalogId),
            events.Select(x => x.Event).ToArray(),
            OperationId.From($"ReplayCatalogProjection:{musicCatalogId.Value}"),
            CancellationToken.None);

        await seedSession.StoreAsync(new CatalogTrackRecordDto
        {
            Id = CatalogTrackRecordDto.GetDocumentId(musicCatalogId.Value),
            TrackId = musicCatalogId.Value,
            Title = "Stale Title",
            ArtistId = "artist_stale",
            AlbumId = "album_stale",
            ArtistName = "Stale Artist",
            AlbumName = "Stale Album",
            NormalizedTitle = "stale title",
            SearchText = "stale title stale artist"
        });
        await seedSession.StoreAsync(new CatalogProjectionCheckpointDocument
        {
            Id = CatalogProjectionCheckpointDocument.GetDocumentId(musicCatalogId.Value),
            MusicCatalogId = musicCatalogId.Value,
            LastAppliedVersion = 99,
            UpdatedAt = Clock
        });
        await seedSession.SaveChangesAsync();

        var session = raven.Store.OpenAsyncSession();
        var projectionHandler = new MusicCatalogChangedHandler(
            new RavenLoadMusicTrackCatalogProjection(session, new RavenMusicTrackCatalogProjectionMapper()),
            new RavenSaveMusicTrackCatalogProjection(session, Soundtrail.Adapters.Registry.TypeTranslationRegistry.Default));
        var handler = new ReplayCatalogProjectionHandler(
            new RavenLoadCatalogProjectionReplayTargets(session),
            new RavenLoadMusicTrackEventsForCatalogReplay(session, TypeTranslationRegistry.Default),
            new RavenResetCatalogProjectionCheckpoint(session),
            projectionHandler);

        return new ReplayCatalogProjectionTestEnvironment(
            handler,
            async id =>
            {
                using var verificationSession = raven.Store.OpenAsyncSession();
                return await verificationSession.LoadAsync<CatalogTrackRecordDto>(
                    CatalogTrackRecordDto.GetDocumentId(id.Value),
                    CancellationToken.None);
            },
            async id =>
            {
                using var verificationSession = raven.Store.OpenAsyncSession();
                var checkpoint = await verificationSession.LoadAsync<CatalogProjectionCheckpointDocument>(
                    CatalogProjectionCheckpointDocument.GetDocumentId(id.Value),
                    CancellationToken.None);
                return checkpoint?.LastAppliedVersion ?? 0;
            },
            async () =>
            {
                using var verificationSession = raven.Store.OpenAsyncSession();
                var tracks = await verificationSession.Advanced.LoadStartingWithAsync<CatalogTrackRecordDto>("catalog/tracks/");
                return tracks.Count();
            },
            new AsyncDisposableAggregate(session, raven));
    }

    private sealed class FakeEventStore(
        IReadOnlyDictionary<string, IReadOnlyList<VersionedMusicTrackEvent>> eventsByMusicCatalogId) :
        ILoadCatalogProjectionReplayTargetsPort,
        ILoadMusicTrackEventsForCatalogReplayPort
    {
        public Task<IReadOnlyList<MusicCatalogId>> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MusicCatalogId>>(
                eventsByMusicCatalogId.Keys.Select(MusicCatalogId.From).ToArray());

        public Task<IReadOnlyList<VersionedMusicTrackEvent>> LoadAsync(
            MusicCatalogId musicCatalogId,
            CancellationToken cancellationToken) =>
            Task.FromResult(eventsByMusicCatalogId.TryGetValue(musicCatalogId.Value, out var events)
                ? events
                : Array.Empty<VersionedMusicTrackEvent>() as IReadOnlyList<VersionedMusicTrackEvent>);
    }

    private sealed class FakeProjectionStore :
        ILoadMusicTrackCatalogProjectionPort,
        ISaveMusicTrackCatalogProjectionPort,
        IResetCatalogProjectionCheckpointPort
    {
        private readonly Dictionary<string, MusicTrackCatalogProjectionSnapshot> projections = new(StringComparer.Ordinal);

        public int TrackDocumentCount => projections.Count;

        public void SeedStale(MusicCatalogId musicCatalogId)
        {
            projections[musicCatalogId.Value] = new MusicTrackCatalogProjectionSnapshot(
                musicCatalogId,
                new CatalogTrackProjection(
                    musicCatalogId.Value,
                    "artist_stale",
                    "album_stale",
                    "Stale Title",
                    "stale title",
                    "Stale Artist",
                    "Stale Album",
                    "stale title stale artist",
                    null,
                    null,
                    null,
                    [],
                    [],
                    [],
                    null,
                    Clock),
                null,
                null,
                99);
        }

        public CatalogTrackRecordDto? LoadTrackDocument(MusicCatalogId musicCatalogId)
        {
            if (!projections.TryGetValue(musicCatalogId.Value, out var snapshot))
            {
                return null;
            }

            return new CatalogTrackRecordDto
            {
                Id = CatalogTrackRecordDto.GetDocumentId(musicCatalogId.Value),
                TrackId = snapshot.Track.TrackId,
                ArtistId = snapshot.Track.ArtistId,
                AlbumId = snapshot.Track.AlbumId,
                Title = snapshot.Track.Title,
                NormalizedTitle = snapshot.Track.NormalizedTitle,
                ArtistName = snapshot.Track.ArtistName,
                AlbumName = snapshot.Track.AlbumName,
                SearchText = snapshot.Track.SearchText,
                MusicBrainzRecordingId = snapshot.Track.MusicBrainzRecordingId,
                Isrc = snapshot.Track.Isrc,
                DurationMs = snapshot.Track.DurationMs,
                UpdatedAt = snapshot.Track.UpdatedAt
            };
        }

        public int LoadCheckpointVersion(MusicCatalogId musicCatalogId) =>
            projections.TryGetValue(musicCatalogId.Value, out var snapshot)
                ? snapshot.ProjectionVersion
                : 0;

        public Task<MusicTrackCatalogProjection> LoadAsync(
            MusicCatalogId musicCatalogId,
            CancellationToken cancellationToken)
        {
            var projection = projections.TryGetValue(musicCatalogId.Value, out var snapshot)
                ? MusicTrackCatalogProjection.Load(snapshot)
                : MusicTrackCatalogProjection.Load(
                    new MusicTrackCatalogProjectionSnapshot(
                        musicCatalogId,
                        new CatalogTrackProjection(
                            musicCatalogId.Value,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            null,
                            null,
                            null,
                            [],
                            [],
                            [],
                            null,
                            default),
                        null,
                        null,
                        0));
            return Task.FromResult(projection);
        }

        public Task SaveAsync(
            MusicTrackCatalogProjection projection,
            CancellationToken cancellationToken)
        {
            projections[projection.MusicCatalogId.Value] = projection.ToSnapshot();
            return Task.CompletedTask;
        }

        public Task ResetAsync(
            MusicCatalogId musicCatalogId,
            CancellationToken cancellationToken)
        {
            projections.Remove(musicCatalogId.Value);
            return Task.CompletedTask;
        }
    }

    private sealed class AsyncDisposableAggregate(
        IAsyncDocumentSession session,
        RavenEmbeddedTestDatabase database) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            session.Dispose();
            database.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private static readonly DateTimeOffset Clock = new(2026, 6, 21, 12, 0, 0, TimeSpan.Zero);
}
