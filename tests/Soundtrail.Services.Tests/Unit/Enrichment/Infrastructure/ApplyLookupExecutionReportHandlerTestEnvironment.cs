using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ApplyLookupExecutionReport;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ApplyLookupExecutionReport.Support;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class ApplyLookupExecutionReportHandlerTestEnvironment
{
    private readonly CatalogSearchDiscoveryRepositoryFake discoveryRepository;

    private ApplyLookupExecutionReportHandlerTestEnvironment()
    {
        Now = new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);
        discoveryRepository = new CatalogSearchDiscoveryRepositoryFake();
        var trackingStore = new CatalogSearchTrackingStoreFake();
        trackingStore.Seed(new CatalogSearchTracking(
            CatalogSearchCriteria.Search("track", "rare unknown song"),
            MusicCatalogId.From("mc_track_1"),
            Now));
        trackingStore.Seed(new CatalogSearchTracking(
            CatalogSearchCriteria.Artist(ArtistId.From("artist_1")),
            MusicCatalogId.From("mc_track_1"),
            Now));

        SeedDiscovery(CatalogSearchCriteria.Search("track", "rare unknown song"));
        SeedDiscovery(CatalogSearchCriteria.Artist(ArtistId.From("artist_1")));

        Handler = new ApplyLookupExecutionReportHandler(
            new CatalogSearchDiscoveryByMusicCatalogIdTransitionApplier(
                trackingStore,
                discoveryRepository));
    }

    public ApplyLookupExecutionReportHandler Handler { get; }

    public DateTimeOffset Now { get; }

    public static ApplyLookupExecutionReportHandlerTestEnvironment Create() => new();

    public IReadOnlyList<IDomainEvent> StoredEvents(string criteria) =>
        discoveryRepository.GetStoredEvents(CatalogSearchCriteria.From(criteria));

    private void SeedDiscovery(CatalogSearchCriteria criteria)
    {
        discoveryRepository.Seed(
            criteria,
            new DiscoveryRequested(
                criteria,
                NormalizedSearchQuery.FromText("rare unknown song"),
                1,
                10,
                Now,
                CorrelationId.From("corr-1")),
            new DiscoveryPlanned(
                criteria,
                LookupPriorityBand.High,
                true,
                30,
                Now.AddSeconds(30),
                "Planner queued lookup",
                Now),
            new DiscoveryStarted(
                criteria,
                LookupPriorityBand.High,
                true,
                "Lookup started",
                Now));
    }
}
