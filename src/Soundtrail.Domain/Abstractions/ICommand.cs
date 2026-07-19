using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Abstractions
{
    public interface ICommand
    {
        CommandId CommandId { get; }
        
        CorrelationId CorrelationId { get; }
        
        DateTimeOffset RequestedAt { get; }
    }
}
