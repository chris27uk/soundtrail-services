namespace Soundtrail.Services.Api.Shared.Adapters;

public sealed record DiscoveryFeedbackResponseDto(
    string Status,
    string Priority,
    DateTimeOffset? NextEligibleAtUtc,
    DateTimeOffset? EarliestExpectedCompletionAtUtc,
    string Reason,
    DateTimeOffset UpdatedAtUtc);
