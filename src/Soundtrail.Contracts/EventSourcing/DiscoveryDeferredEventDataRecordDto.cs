namespace Soundtrail.Contracts.EventSourcing;

public sealed record DiscoveryDeferredEventDataRecordDto(
    string Criteria,
    bool WillBeLookedUp,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason,
    DateTimeOffset DeferredAtUtc);
