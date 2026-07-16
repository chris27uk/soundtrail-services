using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Domain.Discovery.Commands
{
    public record RequestEnrichmentWork(
        EnrichmentTarget Target,
        CommandId CommandId,
        CorrelationId CorrelationId,
        DateTimeOffset CreatedAt) : ICommand;
}
