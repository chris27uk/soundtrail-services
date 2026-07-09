using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Domain.Discovery.Assesment;

public sealed record AssessWorkCommand(
    CommandId CommandId,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    EnrichmentFilter Filter,
    int? TrustLevel = null,
    int? RiskScore = null) : ICommand
{
    public static CommandId Id(EnrichmentFilter filter, DateTimeOffset createdAt) => CommandId.For($"AssessWork:{filter.NormalisedIdentifier}:{createdAt}");
}
