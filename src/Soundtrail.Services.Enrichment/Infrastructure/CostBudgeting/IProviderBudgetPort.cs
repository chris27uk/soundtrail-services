using Soundtrail.Services.Enrichment.Budgets;
using Soundtrail.Services.Enrichment.Models;

namespace Soundtrail.Services.Enrichment.Ports;

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
