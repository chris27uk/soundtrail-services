using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Catalog.Events;

public sealed record StreamingLocationsRequired(
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    CorrelationId CorrelationId,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt,
    MusicSearchCriteria SearchCriteria,
    CatalogTrackHierarchy? Hierarchy = null) : IMusicTrackEvent;
