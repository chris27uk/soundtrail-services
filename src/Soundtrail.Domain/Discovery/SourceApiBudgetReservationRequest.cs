using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Discovery;

public sealed record SourceApiBudgetReservationRequest(
    ProviderName Source,
    DateTimeOffset RequestedAt,
    int RequestedAmount = 1);
