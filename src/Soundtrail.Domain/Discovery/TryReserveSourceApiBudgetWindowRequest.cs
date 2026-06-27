using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Discovery;

public sealed record TryReserveSourceApiBudgetWindowRequest(
    LookupSource Source,
    DateTimeOffset WindowStartedAt,
    DateTimeOffset WindowEndsAt,
    int RequestedAmount,
    int MaxRequests,
    int SafetyMarginPercent,
    string KeyPrefix);
