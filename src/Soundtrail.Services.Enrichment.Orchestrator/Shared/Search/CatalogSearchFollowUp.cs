using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

public sealed class CatalogSearchFollowUp
{
    private CatalogSearchFollowUp(
        bool requiresTrackMetadataLookup,
        IReadOnlyList<StreamingLocationLookupCandidate> streamingLocationLookups)
    {
        RequiresTrackMetadataLookup = requiresTrackMetadataLookup;
        StreamingLocationLookups = streamingLocationLookups;
    }

    public bool RequiresTrackMetadataLookup { get; }
    public IReadOnlyList<StreamingLocationLookupCandidate> StreamingLocationLookups { get; }

    public static CatalogSearchFollowUp TrackMetadataRequired() =>
        new(true, []);

    public static CatalogSearchFollowUp StreamingLocationsRequired(
        IReadOnlyList<StreamingLocationLookupCandidate> streamingLocationLookups) =>
        new(false, streamingLocationLookups);

}

public sealed record StreamingLocationLookupCandidate(
    MusicCatalogId MusicCatalogId,
    MusicSearchCriteria SearchCriteria,
    CatalogTrackHierarchy? Hierarchy);
