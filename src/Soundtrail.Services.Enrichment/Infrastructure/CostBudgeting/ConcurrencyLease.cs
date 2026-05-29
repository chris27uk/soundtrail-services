namespace Soundtrail.Services.Enrichment.Infrastructure.CostBudgeting;

public sealed record ConcurrencyLease(
    string LeaseId,
    ProviderName Provider,
    string LeaseOwner,
    DateTimeOffset ExpiresAt);
