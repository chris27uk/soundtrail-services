using Soundtrail.Services.Enrichment.Models;
using Soundtrail.Services.Enrichment.Ports;

namespace Soundtrail.Services.Enrichment.Budgets;

public sealed class ProviderBudgetService(IProviderBudgetPort budgets)
{
    public Task<ProviderBudgetDecision> TryConsumeAsync(
        ProviderName provider,
        ProviderRateLimitPolicy policy,
        CancellationToken cancellationToken) =>
        budgets.TryConsumeAsync(provider, policy, cancellationToken);
}
