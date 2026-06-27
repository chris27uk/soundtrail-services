using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Discovery;

public sealed record SourceApiBudgetReservationRequest(
    LookupSource Source,
    DateTimeOffset RequestedAt,
    int RequestedAmount = 1);
