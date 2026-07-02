using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicTrack;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class AssessMusicTrackHandlerTestEnvironment
{
    private readonly CatalogSearchDiscoveryRepositoryFake discoveryRepository;
    private readonly LocalMusicTrackSearchFake localSearch;

    private AssessMusicTrackHandlerTestEnvironment()
    {
        discoveryRepository = new CatalogSearchDiscoveryRepositoryFake();
        localSearch = new LocalMusicTrackSearchFake();
        Handler = new AssessMusicTrackHandler(
            discoveryRepository,
            new DiscoveryPriorityPolicy(),
            localSearch);
    }

    public AssessMusicTrackHandler Handler { get; }

    public static AssessMusicTrackHandlerTestEnvironment Create() => new();

    public void SeedDiscoveryRequested(MusicSearchCriteria searchCriteria)
    {
        discoveryRepository.Seed(
            searchCriteria,
            new DiscoveryRequested(
                searchCriteria,
                null,
                1,
                10,
                Clock,
                CorrelationId.From("corr-1")));
    }

    public void SeedCandidateIdentified(
        MusicSearchCriteria searchCriteria,
        MusicCatalogId musicCatalogId,
        int trustLevel = 1,
        int riskScore = 10)
    {
        discoveryRepository.Seed(
            searchCriteria,
            new CatalogCandidateIdentified(
                searchCriteria,
                musicCatalogId,
                trustLevel,
                riskScore,
                Clock,
                CorrelationId.From("corr-1")));
    }

    public void SeedDiscoveryDeferred(
        MusicSearchCriteria searchCriteria,
        DateTimeOffset? earliestExpectedCompletionAt = null)
    {
        discoveryRepository.Seed(
            searchCriteria,
            new DiscoveryDeferred(
                searchCriteria,
                true,
                60,
                earliestExpectedCompletionAt ?? Clock.AddSeconds(60),
                "Planner deferred lookup",
                Clock));
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

    public AssessMusicTrackCommand BacklogCommand(MusicSearchCriteria searchCriteria, MusicCatalogId musicCatalogId) =>
        new(
            AssessMusicTrackCommand.Id(musicCatalogId, Clock),
            CorrelationId.From("corr-1"),
            Clock,
            LookupPriorityBand.Low,
            musicCatalogId,
            searchCriteria);

    private static readonly DateTimeOffset Clock = new(2026, 6, 28, 12, 0, 0, TimeSpan.Zero);
}
