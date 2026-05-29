namespace Soundtrail.Services.Enrichment.Infrastructure.CostBudgeting;

public interface IProviderCircuitStatePort
{
    Task<ProviderCircuitState> GetAsync(
        ProviderName provider,
        CancellationToken cancellationToken);

    Task UpsertAsync(
        ProviderCircuitState state,
        CancellationToken cancellationToken);
}
