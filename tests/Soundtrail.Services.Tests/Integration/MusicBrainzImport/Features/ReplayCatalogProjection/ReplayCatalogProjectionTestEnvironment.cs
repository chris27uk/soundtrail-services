using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
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
        var artistId = ArtistId.From("artist_the_killers");
        var events = new[]
        {
            new VersionedCatalogEvent(
                1,
                new TrackDiscovered(
                    musicCatalogId,
                    "Mr. Brightside",
                    "The Killers",
                    222000,
                    "USIR20400274",
                    "mbid-1",
                    LookupSource.MusicBrainz,
                    Clock)),
            new VersionedCatalogEvent(
                2,
                new ArtistDiscovered(
                    "artist_the_killers",
                    "The Killers",
                    "mb-artist-the-killers",
                    LookupSource.MusicBrainz,
                    Clock.AddMinutes(1))),
            new VersionedCatalogEvent(
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
            ReplayCatalogProjectionMode.InProcessFake => CreateFake(musicCatalogId, artistId, events),
            ReplayCatalogProjectionMode.RavenEmbedded => await CreateRavenAsync(musicCatalogId, artistId, events),
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
        ArtistId artistId,
        IReadOnlyList<VersionedCatalogEvent> events)
    {
        var eventStore = new FakeEventStore(new Dictionary<string, IReadOnlyList<VersionedCatalogEvent>>
        {
            [artistId.Value] = events
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
        ArtistId artistId,
        IReadOnlyList<VersionedCatalogEvent> events)
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        using var seedSession = raven.Store.OpenAsyncSession();
        var repository = new Soundtrail.Adapters.EventSourcing.RavenEventStreamRepository<ArtistId, IDomainEvent>(
            seedSession,
            TypeTranslationRegistry.Default,
            Soundtrail.Adapters.MusicTrackEventStore.ArtistCatalogEventStreamDefinition.Create());
        await repository.AppendAsync(
            LoadedEventStream<ArtistId, IDomainEvent>.Empty(artistId),
            events.Select(x => x.Event).ToArray(),
            OperationId.From($"ReplayCatalogProjection:{artistId.Value}"),
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
            Id = CatalogProjectionCheckpointDocument.GetDocumentId(artistId.Value),
            ArtistId = artistId.Value,
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
                    CatalogProjectionCheckpointDocument.GetDocumentId(artistId.Value),
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
        IReadOnlyDictionary<string, IReadOnlyList<VersionedCatalogEvent>> eventsByMusicCatalogId) :
        ILoadCatalogProjectionReplayTargetsPort,
        ILoadMusicTrackEventsForCatalogReplayPort
    {
        public Task<IReadOnlyList<ArtistId>> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ArtistId>>(
                eventsByMusicCatalogId.Keys.Select(ArtistId.From).ToArray());

        public Task<IReadOnlyList<VersionedCatalogEvent>> LoadAsync(
            ArtistId musicCatalogId,
            CancellationToken cancellationToken) =>
            Task.FromResult(eventsByMusicCatalogId.TryGetValue(musicCatalogId.Value, out var events)
                ? events
                : Array.Empty<VersionedCatalogEvent>() as IReadOnlyList<VersionedCatalogEvent>);
    }

    private sealed class FakeProjectionStore :
        ILoadMusicTrackCatalogProjectionPort,
        ISaveMusicTrackCatalogProjectionPort,
        IResetCatalogProjectionCheckpointPort
    {
        private readonly Dictionary<string, ArtistCatalog> projections = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> checkpointVersions = new(StringComparer.Ordinal);

        public int TrackDocumentCount => projections.Count;

        public void SeedStale(MusicCatalogId musicCatalogId) => _ = musicCatalogId;

        public CatalogTrackRecordDto? LoadTrackDocument(MusicCatalogId musicCatalogId)
        {
            var track = projections.Values
                .SelectMany(x => x.GetTracks())
                .SingleOrDefault(x => x.MusicCatalogId == musicCatalogId);

            return track is null
                ? null
                : new CatalogTrackRecordDto
                {
                    Id = CatalogTrackRecordDto.GetDocumentId(track.MusicCatalogId.Value),
                    TrackId = track.MusicCatalogId.Value,
                    Title = track.Title,
                    ArtistId = track.ArtistId.Value,
                    AlbumId = track.AlbumId?.Value,
                    ArtistName = track.ArtistName,
                    AlbumName = track.AlbumTitle,
                    NormalizedTitle = track.Title.ToLowerInvariant(),
                    SearchText = $"{track.Title} {track.ArtistName}".ToLowerInvariant()
                };
        }

        public int LoadCheckpointVersion(MusicCatalogId musicCatalogId)
        {
            _ = musicCatalogId;
            return checkpointVersions.Values.DefaultIfEmpty(0).Max();
        }

        public Task<MusicTrackCatalogProjection> LoadAsync(MusicCatalogId musicCatalogId, CancellationToken cancellationToken)
        {
            _ = musicCatalogId;
            _ = cancellationToken;
            return Task.FromResult(new MusicTrackCatalogProjection(musicCatalogId));
        }

        public Task SaveAsync(
            ArtistId artistId,
            int version,
            ArtistCatalog projection,
            CancellationToken cancellationToken)
        {
            projections[artistId.Value] = projection;
            checkpointVersions[artistId.Value] = version;
            return Task.CompletedTask;
        }

        public Task ResetAsync(
            ArtistId artistId,
            CancellationToken cancellationToken)
        {
            projections.Remove(artistId.Value);
            checkpointVersions.Remove(artistId.Value);
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
