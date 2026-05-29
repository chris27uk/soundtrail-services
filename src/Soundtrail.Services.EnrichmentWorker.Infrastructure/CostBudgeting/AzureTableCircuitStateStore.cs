using System.Collections.Concurrent;
using Soundtrail.Services.EnrichmentWorker.Models;
using Soundtrail.Services.EnrichmentWorker.Ports;

namespace Soundtrail.Services.EnrichmentWorker.Infrastructure.AzureTable;

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
