namespace Soundtrail.Domain.Responses;

public sealed record RebuildAllReadModelsResult(
    int PlannerMusicTrackReplayedStreamCount,
    int PlannerMusicTrackReplayedEventCount,
    int CatalogReplayedStreamCount,
    int CatalogReplayedEventCount,
    int DiscoveryLifecycleReplayedCriteriaCount,
    int DiscoveryLifecycleReplayedEventCount,
    int ClearedPotentialCatalogLookupWorkCount,
    int ClearedCatalogSearchTrackingCount,
    int ClearedActiveLookupWorkCount);
