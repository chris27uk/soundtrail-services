using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Persistence;

public interface IRecordCatalogSearchStartedPort
{
    Task RecordAsync(
        CatalogSearchCriteria criteria,
        IReadOnlyList<MusicCatalogId> musicCatalogIds,
        int trustLevel,
        int riskScore,
        DateTimeOffset occurredAt,
        CorrelationId correlationId,
        CancellationToken cancellationToken);
}
