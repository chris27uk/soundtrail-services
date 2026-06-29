using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure
{
    internal sealed class CatalogSearchRequestedHandlerTestEnvironment
    {
        private readonly FakeMusicCatalogCandidateSearch search;
        private readonly CatalogSearchDiscoveryRepositoryFake catalogSearchDiscoveryRepositoryFake;

        private CatalogSearchRequestedHandlerTestEnvironment(FakeMusicCatalogCandidateSearch search)
        {
            this.search = search;
            this.catalogSearchDiscoveryRepositoryFake = new CatalogSearchDiscoveryRepositoryFake();
            this.Handler = new SearchCatalogRequestedHandler(
                search,
                this.catalogSearchDiscoveryRepositoryFake);
        }

        public SearchCatalogRequestedHandler Handler { get; }

        public FakeMusicCatalogCandidateSearch Search => this.search;

        public CatalogSearchDiscoveryRepositoryFake DiscoveryRepository => this.catalogSearchDiscoveryRepositoryFake;

        public static CatalogSearchRequestedHandlerTestEnvironment WithNoExistingCandidates() =>
            new(new FakeMusicCatalogCandidateSearch());

        public SearchCatalogRequested Request(
            string query,
            int trustLevel,
            int riskScore,
            DateTimeOffset? occurredAt = null) =>
            new(
                SearchCriteria: MusicSearchCriteria.ByQuery(query, SearchTypesFilter.Tracks),
                Playback: PlaybackProviderFilter.Parse("spotify,appleMusic,youtubeMusic"),
                TrustLevel: trustLevel,
                RiskScore: riskScore,
                OccurredAt: occurredAt ?? new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero),
                CorrelationId: CorrelationId.New());
    }
}
