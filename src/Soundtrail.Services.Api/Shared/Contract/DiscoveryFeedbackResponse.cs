using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Api.Shared.Contract;

public sealed record DiscoveryFeedbackResponse(
    string Status,
    LookupPriorityBand Priority,
    DateTimeOffset? NextEligibleAt,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason,
    DateTimeOffset UpdatedAtUtc);
