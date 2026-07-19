using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Budget;

public sealed record TryReserveSourceApiBudgetWindowRequest(
    LookupSource Source,
    DateTimeOffset WindowStartedAt,
    DateTimeOffset WindowEndsAt,
    int RequestedAmount,
    int MaxRequests,
    int SafetyMarginPercent,
    string KeyPrefix);
