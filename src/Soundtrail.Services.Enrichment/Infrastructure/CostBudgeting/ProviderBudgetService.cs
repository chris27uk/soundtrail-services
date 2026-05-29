namespace Soundtrail.Services.Enrichment.Infrastructure.CostBudgeting;

public sealed class ProviderBudgetService(IProviderBudgetPort budgets)
{
    public Task<ProviderBudgetDecision> TryConsumeAsync(
        ProviderName provider,
        ProviderRateLimitPolicy policy,
        CancellationToken cancellationToken) =>
        budgets.TryConsumeAsync(provider, policy, cancellationToken);
}
