namespace Soundtrail.Domain.Search;

public sealed record SearchDiscovery(
    bool WillBeLookedUp,
    string? Reason,
    int? RetryAfterSeconds);
