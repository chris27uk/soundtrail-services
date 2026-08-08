using Soundtrail.Adapters.Projection;
using Soundtrail.Contracts.EventSourcing;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

internal sealed class CatalogProjectionDispatcher(
    StoredEventDomainEventResolver resolver,
    HandlerCollection handlers)
{
    public Task DispatchAsync(RavenStoredEventRecord storedEvent, CancellationToken cancellationToken)
    {
        return storedEvent.AggregateType switch
        {
            "catalog-stream" => handlers.HandleAsync(resolver.Resolve(storedEvent), cancellationToken),
            _ => throw new InvalidOperationException(
                $"Unsupported catalog aggregate type '{storedEvent.AggregateType}'.")
        };
    }
}
