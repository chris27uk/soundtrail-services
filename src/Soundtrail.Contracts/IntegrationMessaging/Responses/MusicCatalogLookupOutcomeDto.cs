namespace Soundtrail.Contracts.IntegrationMessaging.Responses;

public sealed record MusicCatalogLookupOutcomeDto(
    string Status,
    string? Reason,
    DateTimeOffset? RetryAt,
    int? RetryAfterSeconds);
