using Soundtrail.Services.Enrichment.Infrastructure.CostBudgeting;
using System.Collections.Concurrent;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.CostBudgeting;

public sealed class AzureTableCircuitStateStore : IProviderCircuitStatePort
{
    private readonly ConcurrentDictionary<ProviderName, ProviderCircuitState> stateByProvider = new();

    public Task<ProviderCircuitState> GetAsync(
        ProviderName provider,
        CancellationToken cancellationToken)
    {
        var state = stateByProvider.GetOrAdd(
            provider,
            static providerName => new ProviderCircuitState(providerName, CircuitState.Closed, 0, null, null, null, null));

        return Task.FromResult(state);
    }

    public Task UpsertAsync(
        ProviderCircuitState state,
        CancellationToken cancellationToken)
    {
        stateByProvider[state.Provider] = state;
        return Task.CompletedTask;
    }
}
