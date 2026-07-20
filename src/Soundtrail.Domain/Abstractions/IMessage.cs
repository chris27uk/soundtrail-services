using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Abstractions
{
    public interface IMessage
    {
        MessageId Id { get; }
        
        CorrelationId CorrelationId { get; }
        
        DateTimeOffset RequestedAt { get; }
    }
}
