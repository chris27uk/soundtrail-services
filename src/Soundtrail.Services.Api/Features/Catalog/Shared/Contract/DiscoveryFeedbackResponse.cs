using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

public sealed record DiscoveryFeedbackResponse(
    string Status,
    LookupPriorityBand Priority,
    DateTimeOffset? NextEligibleAt,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason,
    DateTimeOffset UpdatedAtUtc);
