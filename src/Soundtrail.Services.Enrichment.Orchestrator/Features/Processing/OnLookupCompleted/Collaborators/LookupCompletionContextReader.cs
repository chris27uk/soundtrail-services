using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted.Collaborators;

internal static class LookupCompletionContextReader
{
    public static async Task<LookupCompletionContext> ReadAsync(
        IEventStreamRepository<CatalogWorkId> repository,
        CatalogWorkId streamId,
        CancellationToken cancellationToken)
    {
        var stream = await repository.LoadAsync(streamId, cancellationToken);
        var scheduled = stream.Events.OfType<WorkScheduled>().LastOrDefault();
        var requested = stream.Events.OfType<WorkRequested>().LastOrDefault();

        var target = 
            scheduled?.Target
            ?? requested?.Target
            ?? throw new InvalidOperationException($"No discovery target exists for stream '{streamId.StableValue}'.");

        var priority = 
            scheduled?.Priority
            ?? requested?.Priority
            ?? throw new InvalidOperationException($"No discovery priority exists for stream '{streamId.StableValue}'.");

        var correlationId = requested?.CorrelationId ?? CorrelationId.From(streamId.StableValue);
        return new LookupCompletionContext(target, priority, correlationId);
    }
}
