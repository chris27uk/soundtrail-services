using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Messages
{
    public record CatalogLookupCompleted(
        MessageId Id, 
        DateTimeOffset RequestedAt, 
        CorrelationId CorrelationId, 
        LookupResult Result) : IMessage;
}
