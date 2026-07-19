using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted.Extensions;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted;

public sealed class LookupCompletedHandler(IEventStreamRepository<CatalogWorkId> repository) : IHandler<CatalogLookupCompleted>
{
    public async Task Handle(CatalogLookupCompleted request, CancellationToken cancellationToken = default)
    {
        var lookupRequest = request.Result;
        var streamId = lookupRequest.StreamId();
        var historyContext = lookupRequest.ToAggregateContext();
        await using var scope = await DiscoveryHistoryScope.LoadFromEventStreamAsync(repository, streamId, historyContext, cancellationToken);
        
        scope.Aggregate.ApplyLookupResult(lookupRequest);
        
        await scope.Aggregate.SaveAsync(cancellationToken);
    }
}
