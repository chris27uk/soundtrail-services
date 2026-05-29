namespace Soundtrail.Services.EnrichmentWorker.Models;

public sealed record ConcurrencyLease(
    string LeaseId,
    ProviderName Provider,
    string LeaseOwner,
    DateTimeOffset ExpiresAt);
