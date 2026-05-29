namespace Soundtrail.Services.Enrichment.Infrastructure.CostBudgeting;

public sealed class ProviderCircuitBreaker(IProviderCircuitStatePort circuitStatePort)
{
    public async Task<bool> IsOpenAsync(ProviderName provider, CancellationToken cancellationToken)
    {
        var state = await circuitStatePort.GetAsync(provider, cancellationToken);
        return state.State == CircuitState.Open;
    }
}
