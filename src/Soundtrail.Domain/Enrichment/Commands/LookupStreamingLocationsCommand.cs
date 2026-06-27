using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Enrichment.Commands;

public sealed record LookupStreamingLocationsCommand(
    CommandId CommandId,
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    CorrelationId CorrelationId,
    MusicSearchCriteria LookupKey,
    CatalogTrackHierarchy? Hierarchy = null) : IMusicCatalogLookupCommand
{
    public static CommandId Id(MusicCatalogId catalogId) => CommandId.For($"LookupStreamingLocations:{catalogId.Value}");
    
    public ProviderName TargetProvider => ProviderName.Odesli;
}
