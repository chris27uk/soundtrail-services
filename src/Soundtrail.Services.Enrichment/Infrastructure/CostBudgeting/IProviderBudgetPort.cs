namespace Soundtrail.Services.Enrichment.Infrastructure.CostBudgeting;

public interface IProviderBudgetPort
{
    Task<bool> IsAvailableAsync(
        ProviderName provider,
        ProviderRateLimitPolicy policy,
        CancellationToken cancellationToken);

    Task<ProviderBudgetDecision> TryConsumeAsync(
        ProviderName provider,
        ProviderRateLimitPolicy policy,
        CancellationToken cancellationToken);
}
