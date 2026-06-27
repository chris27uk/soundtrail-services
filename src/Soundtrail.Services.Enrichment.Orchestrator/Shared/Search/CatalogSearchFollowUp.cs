using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

public sealed class CatalogSearchFollowUp
{
    private readonly IReadOnlyList<StreamingLocationLookupCandidate> streamingLocationLookups;

    private CatalogSearchFollowUp(
        bool requiresTrackMetadataLookup,
        IReadOnlyList<StreamingLocationLookupCandidate> streamingLocationLookups)
    {
        RequiresTrackMetadataLookup = requiresTrackMetadataLookup;
        this.streamingLocationLookups = streamingLocationLookups;
    }

    public bool RequiresTrackMetadataLookup { get; }

    public static CatalogSearchFollowUp TrackMetadataRequired() =>
        new(true, []);

    public static CatalogSearchFollowUp StreamingLocationsRequired(
        IReadOnlyList<StreamingLocationLookupCandidate> streamingLocationLookups) =>
        new(false, streamingLocationLookups);

    public void AppendTo(
        SearchOrSeekHistory searchHistory,
        int trustLevel,
        int riskScore,
        DateTimeOffset occurredAt,
        CorrelationId correlationId)
    {
        if (RequiresTrackMetadataLookup)
        {
            searchHistory.MetadataRequired(
                trustLevel,
                riskScore,
                occurredAt,
                correlationId);
            return;
        }

        foreach (var lookup in streamingLocationLookups)
        {
            searchHistory.StreamingLocationsRequired(
                lookup.MusicCatalogId,
                LookupPriorityBand.Low,
                occurredAt,
                correlationId,
                lookup.SearchCriteria,
                lookup.Hierarchy);
        }
    }
}

public sealed record StreamingLocationLookupCandidate(
    MusicCatalogId MusicCatalogId,
    MusicSearchCriteria SearchCriteria,
    CatalogTrackHierarchy? Hierarchy);
