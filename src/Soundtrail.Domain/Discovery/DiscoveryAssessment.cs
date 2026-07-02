using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Discovery;

public sealed record DiscoveryAssessment(
    DiscoveryAssessmentAction Action,
    LookupPriorityBand? Priority,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason);
