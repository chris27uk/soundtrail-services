namespace Soundtrail.Tools.MusicBrainzImport.Features.RebuildAllReadModels.OperationalState;

public sealed record ClearPlannerOperationalStateResult(
    int ClearedPotentialCatalogLookupWorkCount,
    int ClearedCatalogSearchTrackingCount,
    int ClearedActiveLookupWorkCount);
