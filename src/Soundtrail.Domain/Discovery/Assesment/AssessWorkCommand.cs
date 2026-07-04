using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Domain.Discovery.Assesment;

public sealed record AssessWorkCommand(
    CommandId CommandId,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    EnrichmentQuery Query,
    int? TrustLevel = null,
    int? RiskScore = null) : ICommand
{
    public static CommandId Id(EnrichmentQuery query, DateTimeOffset createdAt) => CommandId.For($"AssessWork:{query.NormalisedIdentifier}:{createdAt}");
}
