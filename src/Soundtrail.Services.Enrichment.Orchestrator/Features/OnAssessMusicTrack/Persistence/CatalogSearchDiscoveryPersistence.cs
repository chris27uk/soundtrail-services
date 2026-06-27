using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicTrack.Persistence;

public sealed class CatalogSearchDiscoveryPersistence(
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    ICatalogSearchDiscoveryRepository discoveryRepository)
{
    public Task RequestAsync(
        MusicSearchCriteria searchCriteria,
        int trustLevel,
        int riskScore,
        DateTimeOffset requestedAt,
        CorrelationId correlationId,
        CancellationToken cancellationToken) =>
        PersistAsync(
            searchCriteria,
            history => history.Request(
                searchCriteria,
                trustLevel,
                riskScore,
                requestedAt,
                correlationId),
            cancellationToken);

    public async Task ApplyToTrackingsAsync(
        MusicCatalogId musicCatalogId,
        Func<SearchOrSeekHistory, bool> apply,
        CancellationToken cancellationToken)
    {
        var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(musicCatalogId, cancellationToken);
        foreach (var tracking in trackings)
        {
            await PersistAsync(tracking.SearchCriteria, apply, cancellationToken);
        }
    }

    private async Task PersistAsync(
        MusicSearchCriteria searchCriteria,
        Func<SearchOrSeekHistory, bool> apply,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var history = await SearchOrSeekHistory.LoadAsync(discoveryRepository, searchCriteria, cancellationToken);
            if (!apply(history))
            {
                return;
            }

            if (await history.SaveAsync(discoveryRepository, cancellationToken))
            {
                return;
            }
        }

        throw new InvalidOperationException("Unable to persist discovery lifecycle after retry.");
    }
}
