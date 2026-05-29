using Soundtrail.Services.EnrichmentWorker.Models;
using Soundtrail.Services.EnrichmentWorker.Ports;

namespace Soundtrail.Services.EnrichmentWorker.Budgets;

public sealed class ProviderBudgetService(IProviderBudgetPort budgets)
{
    public Task<ProviderBudgetDecision> TryConsumeAsync(
        ProviderName provider,
        ProviderRateLimitPolicy policy,
        CancellationToken cancellationToken) =>
        budgets.TryConsumeAsync(provider, policy, cancellationToken);
}
