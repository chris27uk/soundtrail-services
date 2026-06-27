using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Enrichment.Commands
{
    public record RunDiscoveryBacklogSchedulingCommand(
        CommandId CommandId,
        DateTimeOffset CreatedAt,
        CorrelationId CorrelationId,
        int BatchSize);
}
