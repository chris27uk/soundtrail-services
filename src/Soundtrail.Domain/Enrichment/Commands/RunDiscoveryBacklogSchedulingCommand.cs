using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Domain.Enrichment.Commands
{
    public record RunDiscoveryBacklogSchedulingCommand(
        CommandId CommandId,
        DateTimeOffset CreatedAt,
        CorrelationId CorrelationId,
        int BatchSize,
        LookupPriorityBand Priority = LookupPriorityBand.Low) : ICommand;
}
