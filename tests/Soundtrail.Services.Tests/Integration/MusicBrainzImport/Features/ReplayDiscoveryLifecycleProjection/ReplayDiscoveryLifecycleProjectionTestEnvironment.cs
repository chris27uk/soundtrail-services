using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Commands;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.Adapters;

namespace Soundtrail.Services.Tests.Integration.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection;

internal sealed class ReplayDiscoveryLifecycleProjectionTestEnvironment : IAsyncDisposable
{
    private ReplayDiscoveryLifecycleProjectionTestEnvironment(
        ReplayDiscoveryLifecycleProjectionBatchHandler handler,
        Func<CatalogSearchCriteria, Task<CatalogSearchStatusRecordDto?>> loadStatusAsync,
        Func<CatalogSearchCriteria, Task<int>> loadCheckpointVersionAsync,
        Func<Task<int>> countStatusDocumentsAsync,
        IAsyncDisposable? asyncDisposable = null)
    {
        Handler = handler;
        this.loadStatusAsync = loadStatusAsync;
        this.loadCheckpointVersionAsync = loadCheckpointVersionAsync;
        this.countStatusDocumentsAsync = countStatusDocumentsAsync;
        this.asyncDisposable = asyncDisposable;
    }

    private readonly Func<CatalogSearchCriteria, Task<CatalogSearchStatusRecordDto?>> loadStatusAsync;
    private readonly Func<CatalogSearchCriteria, Task<int>> loadCheckpointVersionAsync;
    private readonly Func<Task<int>> countStatusDocumentsAsync;
    private readonly IAsyncDisposable? asyncDisposable;

    public ReplayDiscoveryLifecycleProjectionBatchHandler Handler { get; }

    public static async Task<ReplayDiscoveryLifecycleProjectionTestEnvironment> CreateAsync(
        ReplayDiscoveryLifecycleProjectionMode mode)
    {
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");

        return mode switch
        {
            ReplayDiscoveryLifecycleProjectionMode.InProcessFake => CreateFake(criteria),
            ReplayDiscoveryLifecycleProjectionMode.RavenEmbedded => await CreateRavenAsync(criteria),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    public Task<CatalogSearchStatusRecordDto?> LoadStatusAsync(CatalogSearchCriteria criteria) =>
        loadStatusAsync(criteria);

    public Task<int> LoadCheckpointVersionAsync(CatalogSearchCriteria criteria) =>
        loadCheckpointVersionAsync(criteria);

    public Task<int> CountStatusDocumentsAsync() =>
        countStatusDocumentsAsync();

    public async ValueTask DisposeAsync()
    {
        if (asyncDisposable is not null)
        {
            await asyncDisposable.DisposeAsync();
        }
    }

    private static ReplayDiscoveryLifecycleProjectionTestEnvironment CreateFake(CatalogSearchCriteria criteria)
    {
        var events = new[]
        {
            new VersionedCatalogSearchDiscoveryEvent(
                1,
                new DiscoveryRequested(
                    criteria,
                    NormalizedSearchQuery.FromText("rare unknown song"),
                    1,
                    10,
                    Clock,
                    CorrelationId.From("corr-1"))),
            new VersionedCatalogSearchDiscoveryEvent(
                2,
                new DiscoveryPlanned(
                    criteria,
                    LookupPriorityBand.High,
                    true,
                    30,
                    null,
                    "Planner queued lookup",
                    Clock.AddSeconds(5)))
        };

        var eventStore = new FakeEventStore(new Dictionary<string, IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>>
        {
            [criteria.Value] = events
        });
        var projectionStore = new FakeProjectionStore();
        projectionStore.SeedStale(criteria);

        var handler = new ReplayDiscoveryLifecycleProjectionBatchHandler(
            eventStore,
            eventStore,
            projectionStore,
            new ProjectDiscoveryLifecycleHandler(projectionStore, projectionStore));

        return new ReplayDiscoveryLifecycleProjectionTestEnvironment(
            handler,
            item => Task.FromResult(projectionStore.LoadStatus(item)),
            item => Task.FromResult(projectionStore.LoadCheckpointVersion(item)),
            () => Task.FromResult(projectionStore.StatusDocumentCount));
    }

    private static async Task<ReplayDiscoveryLifecycleProjectionTestEnvironment> CreateRavenAsync(
        CatalogSearchCriteria criteria)
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        var repository = new RavenCatalogSearchDiscoveryRepository(raven.Store);
        var discovery = await CatalogSearchDiscovery.LoadAsync(repository, criteria, CancellationToken.None);
        discovery.Request(
            new CatalogSearchAttempt(
                criteria,
                NormalizedSearchQuery.FromText("rare unknown song"),
                1,
                10,
                Clock,
                CorrelationId.From("corr-1")));
        await discovery.SaveAsync(repository, CancellationToken.None);

        discovery = await CatalogSearchDiscovery.LoadAsync(repository, criteria, CancellationToken.None);
        discovery.Plan(LookupPriorityBand.High, 30, null, "Planner queued lookup", Clock.AddSeconds(5));
        await discovery.SaveAsync(repository, CancellationToken.None);

        using (var seedSession = raven.Store.OpenAsyncSession())
        {
            await seedSession.StoreAsync(new CatalogSearchStatusRecordDto
            {
                Id = CatalogSearchStatusRecordDto.GetDocumentId(criteria.Value),
                Criteria = criteria.Value,
                Status = "Stale",
                Priority = "Low",
                WillBeLookedUp = false,
                Reason = "Stale status",
                UpdatedAt = Clock
            });
            await seedSession.StoreAsync(new DiscoveryLifecycleProjectionCheckpointDocument
            {
                Id = DiscoveryLifecycleProjectionCheckpointDocument.GetDocumentId(criteria.Value),
                Criteria = criteria.Value,
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
            new ProjectDiscoveryLifecycleHandler(
                new RavenLoadDiscoveryLifecycleProjection(session, new RavenDiscoveryLifecycleProjectionMapper()),
                new RavenSaveDiscoveryLifecycleProjection(session, new RavenDiscoveryLifecycleProjectionMapper())));

        return new ReplayDiscoveryLifecycleProjectionTestEnvironment(
            handler,
            async item =>
            {
                using var verificationSession = raven.Store.OpenAsyncSession();
                return await verificationSession.LoadAsync<CatalogSearchStatusRecordDto>(
                    CatalogSearchStatusRecordDto.GetDocumentId(item.Value),
                    CancellationToken.None);
            },
            async item =>
            {
                using var verificationSession = raven.Store.OpenAsyncSession();
                var checkpoint = await verificationSession.LoadAsync<DiscoveryLifecycleProjectionCheckpointDocument>(
                    DiscoveryLifecycleProjectionCheckpointDocument.GetDocumentId(item.Value),
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
        public Task<IReadOnlyList<CatalogSearchCriteria>> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CatalogSearchCriteria>>(
                eventsByCriteria.Keys.Select(CatalogSearchCriteria.From).ToArray());

        public Task<IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>> LoadAsync(
            CatalogSearchCriteria criteria,
            CancellationToken cancellationToken) =>
            Task.FromResult(eventsByCriteria.TryGetValue(criteria.Value, out var events)
                ? events
                : Array.Empty<VersionedCatalogSearchDiscoveryEvent>() as IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>);
    }

    private sealed class FakeProjectionStore :
        ILoadDiscoveryLifecycleProjectionPort,
        ISaveDiscoveryLifecycleProjectionPort,
        IResetDiscoveryLifecycleProjectionPort
    {
        private readonly Dictionary<string, DiscoveryLifecycleProjectionSnapshot> projections = new(StringComparer.Ordinal);

        public int StatusDocumentCount => projections.Count;

        public void SeedStale(CatalogSearchCriteria criteria)
        {
            projections[criteria.Value] = new DiscoveryLifecycleProjectionSnapshot(
                criteria,
                "Stale",
                "Low",
                false,
                null,
                null,
                "Stale status",
                Clock,
                99);
        }

        public CatalogSearchStatusRecordDto? LoadStatus(CatalogSearchCriteria criteria)
        {
            if (!projections.TryGetValue(criteria.Value, out var snapshot))
            {
                return null;
            }

            return new CatalogSearchStatusRecordDto
            {
                Id = CatalogSearchStatusRecordDto.GetDocumentId(criteria.Value),
                Criteria = criteria.Value,
                Status = snapshot.Status,
                Priority = snapshot.Priority,
                WillBeLookedUp = snapshot.WillBeLookedUp,
                EstimatedRetryAfterSeconds = snapshot.EstimatedRetryAfterSeconds,
                EarliestExpectedCompletionAt = snapshot.EarliestExpectedCompletionAt,
                Reason = snapshot.Reason,
                UpdatedAt = snapshot.UpdatedAt
            };
        }

        public int LoadCheckpointVersion(CatalogSearchCriteria criteria) =>
            projections.TryGetValue(criteria.Value, out var snapshot)
                ? snapshot.ProjectionVersion
                : 0;

        public Task<DiscoveryLifecycleProjection> LoadAsync(
            CatalogSearchCriteria criteria,
            CancellationToken cancellationToken)
        {
            var projection = projections.TryGetValue(criteria.Value, out var snapshot)
                ? DiscoveryLifecycleProjection.Load(snapshot)
                : DiscoveryLifecycleProjection.Load(
                    new DiscoveryLifecycleProjectionSnapshot(
                        criteria,
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
            projections[projection.Criteria.Value] = projection.ToSnapshot();
            return Task.CompletedTask;
        }

        public Task ResetAsync(
            CatalogSearchCriteria criteria,
            CancellationToken cancellationToken)
        {
            projections.Remove(criteria.Value);
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
