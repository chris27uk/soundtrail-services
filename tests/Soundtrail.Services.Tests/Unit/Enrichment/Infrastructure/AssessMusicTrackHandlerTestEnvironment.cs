using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicTrack;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class AssessMusicTrackHandlerTestEnvironment
{
    private readonly CatalogSearchDiscoveryRepositoryFake discoveryRepository;
    private readonly PotentialCatalogLookupWorkStoreFake workStore;
    private readonly CatalogSearchTrackingStoreFake trackingStore;
    private readonly LocalMusicTrackSearchFake localSearch;

    private AssessMusicTrackHandlerTestEnvironment()
    {
        discoveryRepository = new CatalogSearchDiscoveryRepositoryFake();
        workStore = new PotentialCatalogLookupWorkStoreFake();
        trackingStore = new CatalogSearchTrackingStoreFake();
        localSearch = new LocalMusicTrackSearchFake();
        Handler = new AssessMusicTrackHandler(
            workStore,
            trackingStore,
            discoveryRepository,
            new DiscoveryPriorityPolicy(),
            localSearch);
    }

    public AssessMusicTrackHandler Handler { get; }

    public void SeedSummary(CatalogDiscoveryWorkSummary summary) => workStore.Seed(summary);

    public static AssessMusicTrackHandlerTestEnvironment Create() => new();

    public void SeedTracking(MusicSearchCriteria searchCriteria, MusicCatalogId musicCatalogId) =>
        trackingStore.Seed(new CatalogSearchTracking(searchCriteria, musicCatalogId, Clock));

    public void SeedDiscoveryRequested(MusicSearchCriteria searchCriteria)
    {
        discoveryRepository.Seed(
            searchCriteria,
            new Soundtrail.Domain.Discovery.Events.DiscoveryRequested(
                searchCriteria,
                null,
                1,
                10,
                Clock,
                CorrelationId.From("corr-1")));
    }

    public void SeedPlayableTrack(MusicCatalogId musicCatalogId)
    {
        localSearch.Seed(new LocalMusicTrackSearchResult(
            musicCatalogId,
            "Rare Unknown Song",
            "Test Artist",
            "Rare Album",
            Isrc: null,
            Mbid: null,
            DurationMs: null,
            IsPlayable: true,
            AvailableProviders: [ProviderName.Spotify],
            ReleaseDate: null));
    }

    public IReadOnlyList<IDomainEvent> StoredEvents(MusicSearchCriteria searchCriteria) =>
        discoveryRepository.GetStoredEvents(searchCriteria);

    public AssessMusicTrackCommand ImmediateCommand(
        MusicSearchCriteria searchCriteria,
        MusicCatalogId musicCatalogId,
        int trustLevel = 1,
        int riskScore = 10) =>
        new(
            AssessMusicTrackCommand.Id(musicCatalogId, Clock),
            CorrelationId.From("corr-1"),
            Clock,
            LookupPriorityBand.Low,
            musicCatalogId,
            searchCriteria,
            trustLevel,
            riskScore);

    public AssessMusicTrackCommand BacklogCommand(MusicCatalogId musicCatalogId) =>
        new(
            AssessMusicTrackCommand.Id(musicCatalogId, Clock),
            CorrelationId.From("corr-1"),
            Clock,
            LookupPriorityBand.Low,
            musicCatalogId);

    private static readonly DateTimeOffset Clock = new(2026, 6, 28, 12, 0, 0, TimeSpan.Zero);
}
