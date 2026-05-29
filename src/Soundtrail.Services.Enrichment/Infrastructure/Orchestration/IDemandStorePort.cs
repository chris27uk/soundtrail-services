using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Enrichment.Infrastructure.Orchestration;

public interface IDemandStorePort
{
    Task<IReadOnlyList<ResolutionDemand>> GetUnresolvedAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<ResolutionDemand?> GetAsync(
        QueryId queryId,
        CancellationToken cancellationToken);

    Task UpsertAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken);

    Task MarkResolvedAsync(
        QueryId queryId,
        CancellationToken cancellationToken);
}
