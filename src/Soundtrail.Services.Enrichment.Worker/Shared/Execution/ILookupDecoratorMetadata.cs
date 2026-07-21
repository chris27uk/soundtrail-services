using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Worker.Shared.Execution;

public interface ILookupDecoratorMetadata<in TMessage>
    where TMessage : IMessage
{
    LookupSource Source { get; }

    LookupResultContext CreateContext(TMessage message);

    CatalogItem CreateExistingItem(TMessage message, DateTimeOffset observedAt);
}
