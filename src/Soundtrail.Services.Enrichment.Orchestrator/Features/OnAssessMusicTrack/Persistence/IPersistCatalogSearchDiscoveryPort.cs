using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicTrack.Persistence;

public interface IPersistCatalogSearchDiscoveryPort
{
    Task RequestAsync(
        CatalogSearchCriteria criteria,
        int trustLevel,
        int riskScore,
        DateTimeOffset requestedAt,
        CorrelationId correlationId,
        CancellationToken cancellationToken);

    Task ApplyToTrackingsAsync(
        MusicCatalogId musicCatalogId,
        Func<CatalogSearchDiscovery, bool> apply,
        CancellationToken cancellationToken);
}
