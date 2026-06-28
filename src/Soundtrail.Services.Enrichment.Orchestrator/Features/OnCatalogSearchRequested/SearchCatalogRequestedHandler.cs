using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Support;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested;

public sealed class SearchCatalogRequestedHandler(
    IMusicCatalogCandidateSearch musicCatalogCandidateSearch,
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository) : IHandler<SearchCatalogRequested>
{
    public async Task Handle(
        SearchCatalogRequested requested,
        CancellationToken cancellationToken = default)
    {
        var loaded = await SearchDiscoveryHistory.LoadAsync(
            discoveryRepository,
            requested.SearchCriteria,
            cancellationToken);

        loaded.Aggregate.SearchRequested(requested);

        var matches = await musicCatalogCandidateSearch.SearchAsync(requested.SearchCriteria, cancellationToken);
        var selectedMatches = new MusicTrackSearchMatchCollection(matches)
            .Query(requested.SearchCriteria);

        if (selectedMatches.Count == 0)
        {
            loaded.Aggregate.IdentifyCatalogCandidate(
                SyntheticCatalogCandidateId.ForSearch(requested.SearchCriteria),
                requested.TrustLevel,
                requested.RiskScore,
                requested.OccurredAt,
                requested.CorrelationId);
        }
        else
        {
            foreach (var selectedMatch in selectedMatches)
            {
                loaded.Aggregate.IdentifyCatalogCandidate(
                    selectedMatch.MusicCatalogId,
                    requested.TrustLevel,
                    requested.RiskScore,
                    requested.OccurredAt,
                    requested.CorrelationId);
            }
        }

        await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
    }
}
