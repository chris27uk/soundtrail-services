using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Candidates;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicDataRequested;

public sealed class OnMusicDataRequestedHandler(
    ISearchForCandidates searchForCandidates,
    IEventStreamRepository<CatalogSearchId, IDomainEvent> repository) : IHandler<SearchForCatalogItemsCommand>
{
    public async Task Handle(SearchForCatalogItemsCommand request, CancellationToken cancellationToken = default)
    {
        if (request.Target is not EnrichmentTarget.Unknown(var searchCriteria))
        {
            throw new InvalidOperationException("OnMusicDataRequested requires a search-criteria enrichment filter.");
        }

        var context = new DiscoveryHistory.SearchRequestContext(request.TrustLevel, request.RiskScore, request.RequestedAt, request.CorrelationId);
        var (stream, aggregate) = await DiscoveryHistory.LoadAsync(repository, searchCriteria, context, cancellationToken);
        var qualifiedCandidates = searchForCandidates.Search(request.Target);

        qualifiedCandidates.Match(
            m => aggregate.NewSearchWithExistingCatalogItems(m.Value), 
            _ => aggregate.NewSearch(searchCriteria));

        await aggregate.SaveAsync(repository, stream, request.CommandId, cancellationToken);
    }
}
