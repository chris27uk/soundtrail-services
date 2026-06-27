using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Persistence;

public interface IRecordCatalogSearchStartedPort
{
    Task RecordAsync(
        MusicSearchCriteria criteria,
        IReadOnlyList<MusicCatalogId> musicCatalogIds,
        int trustLevel,
        int riskScore,
        DateTimeOffset occurredAt,
        CorrelationId correlationId,
        CancellationToken cancellationToken);
}
