using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Adapters.Projection;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

internal sealed class DiscoveryProjectionDispatcher(
    StoredEventDomainEventResolver resolver,
    HandlerCollection handlers)
{
    public Task DispatchAsync(RavenStoredEventRecord storedEvent, CancellationToken cancellationToken) =>
        handlers.HandleAsync(resolver.Resolve(storedEvent), cancellationToken);
}
