namespace Soundtrail.Services.Enrichment.Models;

public sealed record ConcurrencyLease(
    string LeaseId,
    ProviderName Provider,
    string LeaseOwner,
    DateTimeOffset ExpiresAt);
