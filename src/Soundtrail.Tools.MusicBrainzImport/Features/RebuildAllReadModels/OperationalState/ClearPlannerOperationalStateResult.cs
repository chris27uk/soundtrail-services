namespace Soundtrail.Tools.MusicBrainzImport.Features.RebuildAllReadModels;

public sealed record ClearPlannerOperationalStateResult(
    int ClearedPotentialCatalogLookupWorkCount,
    int ClearedCatalogSearchTrackingCount,
    int ClearedActiveLookupWorkCount);
