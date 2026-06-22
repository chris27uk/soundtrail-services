using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.ProjectionModel;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ProjectDiscoveryLifecycle;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ProjectMusicTrackProjection;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;
using Soundtrail.Tools.MusicBrainzImport.Features.RebuildAllReadModels;
using Soundtrail.Tools.MusicBrainzImport.Features.RebuildAllReadModels.Adapters;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayPlannerMusicTrackProjection;

namespace Soundtrail.Services.Tests.Unit.MusicBrainzImport.Features.RebuildAllReadModels;

public sealed class RebuildAllReadModelsHandlerTests
{
    [Fact]
    public async Task Given_Persisted_State_When_Rebuild_All_Is_Run_Then_Planner_State_Is_Cleared_And_All_Read_Models_Are_Replayed()
    {
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        var criteria = CatalogSearchCriteria.Search("track", "mr brightside killers");

        var trackEvents = new[]
        {
            new VersionedMusicTrackEvent(
                1,
                new TrackDiscovered(
                    "Mr. Brightside",
                    "The Killers",
                    222000,
                    "USIR20400274",
                    "mbid-1",
                    ProviderName.MusicBrainz,
                    Clock))
        };
        var discoveryEvents = new[]
        {
            new VersionedCatalogSearchDiscoveryEvent(
                1,
                new DiscoveryPlanned(
                    criteria,
                    LookupPriorityBand.High,
                    true,
                    30,
                    Clock.AddMinutes(2),
                    "Planner queued lookup",
                    Clock.AddMinutes(1)))
        };

        var plannerEventStore = new FakeMusicTrackReplayEventStore(new Dictionary<string, IReadOnlyList<VersionedMusicTrackEvent>>
        {
            [musicCatalogId.Value] = trackEvents
        });
        var plannerProjectionStore = new MusicTrackProjectionStoreFake();
        var plannerResetPort = new FakePlannerMusicTrackProjectionResetPort();
        var plannerReplayHandler = new ReplayPlannerMusicTrackProjectionBatchHandler(
            plannerEventStore,
            plannerEventStore,
            plannerResetPort,
            new ProjectMusicTrackProjectionHandler(plannerProjectionStore, plannerProjectionStore));

        var catalogEventStore = new FakeMusicTrackReplayEventStore(new Dictionary<string, IReadOnlyList<VersionedMusicTrackEvent>>
        {
            [musicCatalogId.Value] = trackEvents
        });
        var catalogProjectionStore = new FakeCatalogProjectionStore();
        var catalogReplayHandler = new ReplayCatalogProjectionHandler(
            catalogEventStore,
            catalogEventStore,
            catalogProjectionStore,
            new ProjectMusicTrackCatalogHandler(catalogProjectionStore, catalogProjectionStore));

        var discoveryEventStore = new FakeDiscoveryReplayEventStore(new Dictionary<string, IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>>
        {
            [criteria.Value] = discoveryEvents
        });
        var discoveryProjectionStore = new FakeDiscoveryLifecycleProjectionStore();
        var discoveryReplayHandler = new ReplayDiscoveryLifecycleProjectionBatchHandler(
            discoveryEventStore,
            discoveryEventStore,
            discoveryProjectionStore,
            new ProjectDiscoveryLifecycleHandler(discoveryProjectionStore, discoveryProjectionStore));

        var clearPlannerOperationalStatePort = new FakeClearPlannerOperationalStatePort(3, 4, 5);
        var handler = new RebuildAllReadModelsHandler(
            plannerReplayHandler,
            catalogReplayHandler,
            discoveryReplayHandler,
            clearPlannerOperationalStatePort);

        await handler.Handle(new RebuildAllReadModelsCommand(), CancellationToken.None);

        plannerResetPort.ResetCatalogIds.Should().ContainSingle().Which.Should().Be(musicCatalogId);
        catalogProjectionStore.ResetCatalogIds.Should().ContainSingle().Which.Should().Be(musicCatalogId);
        discoveryProjectionStore.ResetCriteria.Should().ContainSingle().Which.Should().Be(criteria);
        clearPlannerOperationalStatePort.WasCalled.Should().BeTrue();

        plannerProjectionStore.Projections[musicCatalogId.Value].Title.Should().Be("Mr. Brightside");
        catalogProjectionStore.Projections[musicCatalogId.Value].Track.Title.Should().Be("Mr. Brightside");
        discoveryProjectionStore.Projections[criteria.Value].Status.Should().Be(CatalogSearchLifecycleStatus.Planned.ToString());
        discoveryProjectionStore.Projections[criteria.Value].Reason.Should().Be("Planner queued lookup");
    }

    private static readonly DateTimeOffset Clock = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

    private sealed class FakeMusicTrackReplayEventStore(
        IReadOnlyDictionary<string, IReadOnlyList<VersionedMusicTrackEvent>> eventsByCatalogId) :
        ILoadCatalogProjectionReplayTargetsPort,
        ILoadMusicTrackEventsForCatalogReplayPort
    {
        public Task<IReadOnlyList<MusicCatalogId>> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MusicCatalogId>>(
                eventsByCatalogId.Keys.Select(MusicCatalogId.From).ToArray());

        public Task<IReadOnlyList<VersionedMusicTrackEvent>> LoadAsync(
            MusicCatalogId musicCatalogId,
            CancellationToken cancellationToken) =>
            Task.FromResult(eventsByCatalogId.TryGetValue(musicCatalogId.Value, out var events)
                ? events
                : Array.Empty<VersionedMusicTrackEvent>() as IReadOnlyList<VersionedMusicTrackEvent>);
    }

    private sealed class FakeDiscoveryReplayEventStore(
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

    private sealed class FakePlannerMusicTrackProjectionResetPort : IResetPlannerMusicTrackProjectionPort
    {
        public List<MusicCatalogId> ResetCatalogIds { get; } = [];

        public Task ResetAsync(MusicCatalogId musicCatalogId, CancellationToken cancellationToken)
        {
            ResetCatalogIds.Add(musicCatalogId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCatalogProjectionStore :
        ILoadMusicTrackCatalogProjectionPort,
        ISaveMusicTrackCatalogProjectionPort,
        IResetCatalogProjectionCheckpointPort
    {
        private readonly Dictionary<string, MusicTrackCatalogProjection> projections = new(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, MusicTrackCatalogProjection> Projections => projections;

        public List<MusicCatalogId> ResetCatalogIds { get; } = [];

        public Task<MusicTrackCatalogProjection> LoadAsync(
            MusicCatalogId musicCatalogId,
            CancellationToken cancellationToken)
        {
            if (!projections.TryGetValue(musicCatalogId.Value, out var projection))
            {
                projection = new MusicTrackCatalogProjection(musicCatalogId);
                projections[musicCatalogId.Value] = projection;
            }

            return Task.FromResult(projection);
        }

        public Task SaveAsync(
            MusicTrackCatalogProjection projection,
            CancellationToken cancellationToken)
        {
            projections[projection.MusicCatalogId.Value] = projection;
            return Task.CompletedTask;
        }

        public Task ResetAsync(
            MusicCatalogId musicCatalogId,
            CancellationToken cancellationToken)
        {
            ResetCatalogIds.Add(musicCatalogId);
            projections.Remove(musicCatalogId.Value);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDiscoveryLifecycleProjectionStore :
        ILoadDiscoveryLifecycleProjectionPort,
        ISaveDiscoveryLifecycleProjectionPort,
        IResetDiscoveryLifecycleProjectionPort
    {
        private readonly Dictionary<string, DiscoveryLifecycleProjection> projections = new(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, DiscoveryLifecycleProjection> Projections => projections;

        public List<CatalogSearchCriteria> ResetCriteria { get; } = [];

        public Task<DiscoveryLifecycleProjection> LoadAsync(
            CatalogSearchCriteria criteria,
            CancellationToken cancellationToken)
        {
            if (!projections.TryGetValue(criteria.Value, out var projection))
            {
                projection = new DiscoveryLifecycleProjection(criteria);
                projections[criteria.Value] = projection;
            }

            return Task.FromResult(projection);
        }

        public Task SaveAsync(
            DiscoveryLifecycleProjection projection,
            CancellationToken cancellationToken)
        {
            projections[projection.Criteria.Value] = projection;
            return Task.CompletedTask;
        }

        public Task ResetAsync(
            CatalogSearchCriteria criteria,
            CancellationToken cancellationToken)
        {
            ResetCriteria.Add(criteria);
            projections.Remove(criteria.Value);
            return Task.CompletedTask;
        }
    }

        private sealed class FakeClearPlannerOperationalStatePort(
            int potentialCatalogLookupWorkCount,
            int catalogSearchTrackingCount,
            int activeLookupWorkCount) : IClearPlannerOperationalStatePort
    {
        public bool WasCalled { get; private set; }

        public int PotentialCatalogLookupWorkCount => potentialCatalogLookupWorkCount;

        public int CatalogSearchTrackingCount => catalogSearchTrackingCount;

        public int ActiveLookupWorkCount => activeLookupWorkCount;

        public Task<ClearPlannerOperationalStateResult> ClearAsync(CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(
                new ClearPlannerOperationalStateResult(
                    potentialCatalogLookupWorkCount,
                    catalogSearchTrackingCount,
                    activeLookupWorkCount));
        }
    }
}
