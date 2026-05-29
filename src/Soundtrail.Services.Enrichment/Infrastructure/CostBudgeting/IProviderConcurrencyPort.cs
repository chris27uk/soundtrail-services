namespace Soundtrail.Services.Enrichment.Infrastructure.CostBudgeting;

public interface IProviderConcurrencyPort
{
    Task<bool> IsAvailableAsync(
        ProviderName provider,
        CancellationToken cancellationToken);

    Task<ConcurrencyLease?> TryAcquireAsync(
        ProviderName provider,
        CancellationToken cancellationToken);

    Task ReleaseAsync(
        ConcurrencyLease lease,
        CancellationToken cancellationToken);
}
