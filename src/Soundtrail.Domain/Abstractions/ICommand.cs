using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Abstractions
{
    public interface ICommand
    {
        CommandId CommandId { get; }
        
        CorrelationId CorrelationId { get; }
        
        DateTimeOffset CreatedAt { get; }
        
        LookupPriorityBand Priority { get; }
    }
}
