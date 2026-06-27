using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.Adapters;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.EventStore;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.ProjectionReset;

namespace Soundtrail.Services.Tests.Integration.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection;

internal sealed class ReplayDiscoveryLifecycleProjectionTestEnvironment : IAsyncDisposable
{
    private ReplayDiscoveryLifecycleProjectionTestEnvironment(
        ReplayDiscoveryLifecycleProjectionBatchHandler handler,
        Func<MusicSearchCriteria, Task<CatalogSearchStatusRecordDto?>> loadStatusAsync,
        Func<MusicSearchCriteria, Task<int>> loadCheckpointVersionAsync,
        Func<Task<int>> countStatusDocumentsAsync,
        IAsyncDisposable? asyncDisposable = null)
    {
        Handler = handler;
        this.loadStatusAsync = loadStatusAsync;
        this.loadCheckpointVersionAsync = loadCheckpointVersionAsync;
        this.countStatusDocumentsAsync = countStatusDocumentsAsync;
        this.asyncDisposable = asyncDisposable;
    }

    private readonly Func<MusicSearchCriteria, Task<CatalogSearchStatusRecordDto?>> loadStatusAsync;
    private readonly Func<MusicSearchCriteria, Task<int>> loadCheckpointVersionAsync;
    private readonly Func<Task<int>> countStatusDocumentsAsync;
    private readonly IAsyncDisposable? asyncDisposable;

    public ReplayDiscoveryLifecycleProjectionBatchHandler Handler { get; }

    public static async Task<ReplayDiscoveryLifecycleProjectionTestEnvironment> CreateAsync(
        ReplayDiscoveryLifecycleProjectionMode mode)
    {
        var searchTerm = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);

        return mode switch
        {
            ReplayDiscoveryLifecycleProjectionMode.InProcessFake => CreateFake(searchTerm),
            ReplayDiscoveryLifecycleProjectionMode.RavenEmbedded => await CreateRavenAsync(searchTerm),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    public Task<CatalogSearchStatusRecordDto?> LoadStatusAsync(MusicSearchCriteria searchCriteria) =>
        this.loadStatusAsync(searchCriteria);

    public Task<int> LoadCheckpointVersionAsync(MusicSearchCriteria searchCriteria) =>
        this.loadCheckpointVersionAsync(searchCriteria);

    public Task<int> CountStatusDocumentsAsync() =>
        this.countStatusDocumentsAsync();

    public async ValueTask DisposeAsync()
    {
        if (this.asyncDisposable is not null)
        {
            await this.asyncDisposable.DisposeAsync();
        }
    }

    private static ReplayDiscoveryLifecycleProjectionTestEnvironment CreateFake(MusicSearchCriteria searchCriteria)
    {
        var events = new[]
        {
            new VersionedCatalogSearchDiscoveryEvent(
                1,
                new DiscoveryRequested(
                    searchCriteria,
                    1,
                    10,
                    Clock,
                    CorrelationId.From("corr-1"))),
            new VersionedCatalogSearchDiscoveryEvent(
                2,
                new DiscoveryPlanned(
                    searchCriteria,
                    LookupPriorityBand.High,
                    true,
                    30,
                    null,
                    "Planner queued lookup",
                    Clock.AddSeconds(5)))
        };

        var persistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria);
        var eventStore = new FakeEventStore(new Dictionary<string, IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>>
        {
            [persistentId] = events
        });
        var projectionStore = new FakeProjectionStore();
        projectionStore.SeedStale(searchCriteria);

        var handler = new ReplayDiscoveryLifecycleProjectionBatchHandler(
            eventStore,
            eventStore,
            projectionStore,
            new CatalogSearchStatusChangedHandler(
                projectionStore,
                projectionStore));

        return new ReplayDiscoveryLifecycleProjectionTestEnvironment(
            handler,
            item => Task.FromResult(projectionStore.LoadStatus(item)),
            item => Task.FromResult(projectionStore.LoadCheckpointVersion(item)),
            () => Task.FromResult(projectionStore.StatusDocumentCount));
    }

    private static async Task<ReplayDiscoveryLifecycleProjectionTestEnvironment> CreateRavenAsync(
        MusicSearchCriteria searchCriteria)
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        var repository = new RavenCatalogSearchDiscoveryRepository(raven.Store);
        var discovery = await SearchOrSeekHistory.LoadAsync(repository, searchCriteria, CancellationToken.None);
        discovery.SearchRequested(
            new SearchCatalogRequested(
                searchCriteria,
                PlaybackProviderFilter.Parse("spotify,appleMusic,youtubeMusic"),
                1,
                10,
                Clock,
                CorrelationId.From("corr-1")));
        await discovery.SaveAsync(repository, CancellationToken.None);

        discovery = await SearchOrSeekHistory.LoadAsync(repository, searchCriteria, CancellationToken.None);
        discovery.Plan(LookupPriorityBand.High, 30, null, "Planner queued lookup", Clock.AddSeconds(5));
        await discovery.SaveAsync(repository, CancellationToken.None);

        var persistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria);
        using (var seedSession = raven.Store.OpenAsyncSession())
        {
            await seedSession.StoreAsync(new CatalogSearchStatusRecordDto
            {
                Id = CatalogSearchStatusRecordDto.GetDocumentId(persistentId),
                Criteria = persistentId,
                Status = "Stale",
                Priority = "Low",
                WillBeLookedUp = false,
                Reason = "Stale status",
                UpdatedAt = Clock
            });
            await seedSession.StoreAsync(new DiscoveryLifecycleProjectionCheckpointDocument
            {
                Id = DiscoveryLifecycleProjectionCheckpointDocument.GetDocumentId(persistentId),
                Criteria = persistentId,
                LastAppliedVersion = 99,
                UpdatedAt = Clock
            });
            await seedSession.SaveChangesAsync(CancellationToken.None);
        }

        var session = raven.Store.OpenAsyncSession();
        var handler = new ReplayDiscoveryLifecycleProjectionBatchHandler(
            new RavenLoadDiscoveryLifecycleReplayTargets(session),
            new RavenLoadDiscoveryLifecycleEventsForReplay(session),
            new RavenResetDiscoveryLifecycleProjection(session),
            new CatalogSearchStatusChangedHandler(
                new RavenLoadDiscoveryLifecycleProjection(session, new RavenDiscoveryLifecycleProjectionMapper()),
                new RavenSaveDiscoveryLifecycleProjection(session, Soundtrail.Translators.Registry.TypeTranslationRegistry.Default)));

        return new ReplayDiscoveryLifecycleProjectionTestEnvironment(
            handler,
            async item =>
            {
                using var verificationSession = raven.Store.OpenAsyncSession();
                return await verificationSession.LoadAsync<CatalogSearchStatusRecordDto>(
                    CatalogSearchStatusRecordDto.GetDocumentId(MusicSearchTermPersistentIdTranslator.ToPersistentId(item)),
                    CancellationToken.None);
            },
            async item =>
            {
                using var verificationSession = raven.Store.OpenAsyncSession();
                var checkpoint = await verificationSession.LoadAsync<DiscoveryLifecycleProjectionCheckpointDocument>(
                    DiscoveryLifecycleProjectionCheckpointDocument.GetDocumentId(MusicSearchTermPersistentIdTranslator.ToPersistentId(item)),
                    CancellationToken.None);
                return checkpoint?.LastAppliedVersion ?? 0;
            },
            async () =>
            {
                using var verificationSession = raven.Store.OpenAsyncSession();
                var statuses = await verificationSession.Advanced.LoadStartingWithAsync<CatalogSearchStatusRecordDto>("catalog-search-status/");
                return statuses.Count();
            },
            new AsyncDisposableAggregate(session, raven));
    }

    private sealed class FakeEventStore(
        IReadOnlyDictionary<string, IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>> eventsByCriteria) :
        ILoadDiscoveryLifecycleReplayTargetsPort,
        ILoadDiscoveryLifecycleEventsForReplayPort
    {
        public Task<IReadOnlyList<MusicSearchCriteria>> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MusicSearchCriteria>>(
                eventsByCriteria.Keys.Select(MusicSearchTermPersistentIdTranslator.ToDomainObject).ToArray());

        public Task<IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>> LoadAsync(
            MusicSearchCriteria searchCriteria,
            CancellationToken cancellationToken) =>
            Task.FromResult(eventsByCriteria.TryGetValue(MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria), out var events)
                ? events
                : Array.Empty<VersionedCatalogSearchDiscoveryEvent>() as IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>);
    }

    private sealed class FakeProjectionStore :
        ILoadDiscoveryLifecycleProjectionPort,
        ISaveDiscoveryLifecycleProjectionPort,
        IResetDiscoveryLifecycleProjectionPort
    {
        private readonly Dictionary<string, DiscoveryLifecycleProjectionSnapshot> projections = new(StringComparer.Ordinal);

        public int StatusDocumentCount => this.projections.Count;

        public void SeedStale(MusicSearchCriteria searchCriteria)
        {
            this.projections[MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria)] = new DiscoveryLifecycleProjectionSnapshot(
                searchCriteria,
                "Stale",
                "Low",
                false,
                null,
                null,
                "Stale status",
                Clock,
                99);
        }

        public CatalogSearchStatusRecordDto? LoadStatus(MusicSearchCriteria searchCriteria)
        {
            var persistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria);
            if (!this.projections.TryGetValue(persistentId, out var snapshot))
            {
                return null;
            }

            return new CatalogSearchStatusRecordDto
            {
                Id = CatalogSearchStatusRecordDto.GetDocumentId(persistentId),
                Criteria = persistentId,
                Status = snapshot.Status,
                Priority = snapshot.Priority,
                WillBeLookedUp = snapshot.WillBeLookedUp,
                EstimatedRetryAfterSeconds = snapshot.EstimatedRetryAfterSeconds,
                EarliestExpectedCompletionAt = snapshot.EarliestExpectedCompletionAt,
                Reason = snapshot.Reason,
                UpdatedAt = snapshot.UpdatedAt
            };
        }

        public int LoadCheckpointVersion(MusicSearchCriteria searchCriteria) =>
            this.projections.TryGetValue(MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria), out var snapshot)
                ? snapshot.ProjectionVersion
                : 0;

        public Task<DiscoveryLifecycleProjection> LoadAsync(
            MusicSearchCriteria searchCriteria,
            CancellationToken cancellationToken)
        {
            var persistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria);
            var projection = this.projections.TryGetValue(persistentId, out var snapshot)
                ? DiscoveryLifecycleProjection.Load(snapshot)
                : DiscoveryLifecycleProjection.Load(
                    new DiscoveryLifecycleProjectionSnapshot(
                        searchCriteria,
                        string.Empty,
                        string.Empty,
                        false,
                        null,
                        null,
                        null,
                        default,
                        0));
            return Task.FromResult(projection);
        }

        public Task SaveAsync(
            DiscoveryLifecycleProjection projection,
            CancellationToken cancellationToken)
        {
            this.projections[MusicSearchTermPersistentIdTranslator.ToPersistentId(projection.SearchCriteria)] = projection.ToSnapshot();
            return Task.CompletedTask;
        }

        public Task ResetAsync(
            MusicSearchCriteria searchCriteria,
            CancellationToken cancellationToken)
        {
            this.projections.Remove(MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria));
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

    private static readonly DateTimeOffset Clock = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
}
