using Soundtrail.Services.Enrichment.Models;
using Soundtrail.Services.Enrichment.Ports;

namespace Soundtrail.Services.Enrichment.Budgets;

public sealed class ProviderCircuitBreaker(IProviderCircuitStatePort circuitStatePort)
{
    public async Task<bool> IsOpenAsync(ProviderName provider, CancellationToken cancellationToken)
    {
        var state = await circuitStatePort.GetAsync(provider, cancellationToken);
        return state.State == CircuitState.Open;
    }
}
