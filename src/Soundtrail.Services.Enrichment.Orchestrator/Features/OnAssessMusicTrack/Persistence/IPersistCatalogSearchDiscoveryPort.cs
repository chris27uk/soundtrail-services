using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicTrack.Persistence;

public interface IPersistCatalogSearchDiscoveryPort
{
    Task RequestAsync(
        MusicSearchCriteria searchCriteria,
        int trustLevel,
        int riskScore,
        DateTimeOffset requestedAt,
        CorrelationId correlationId,
        CancellationToken cancellationToken);

    Task ApplyToTrackingsAsync(
        MusicCatalogId musicCatalogId,
        Func<SearchOrSeekHistory, bool> apply,
        CancellationToken cancellationToken);
}
