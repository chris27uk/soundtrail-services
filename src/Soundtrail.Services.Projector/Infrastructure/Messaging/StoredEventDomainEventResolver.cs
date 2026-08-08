using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

internal sealed class StoredEventDomainEventResolver(ITypeRegistry typeRegistry)
{
    public IDomainEvent Resolve(RavenStoredEventRecord storedEvent)
    {
        if (storedEvent.Body is null)
        {
            throw new InvalidOperationException($"Stored event '{storedEvent.Id}' is missing a body.");
        }

        return (IDomainEvent)typeRegistry.ToDomainObject(storedEvent.Body);
    }
}
