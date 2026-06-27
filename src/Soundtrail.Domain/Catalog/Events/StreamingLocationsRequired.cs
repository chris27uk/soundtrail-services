using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Events;

public sealed record StreamingLocationsRequired(
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    CorrelationId CorrelationId,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt,
    MusicSearchCriteria SearchCriteria,
    CatalogTrackHierarchy? Hierarchy = null) : IMusicTrackEvent;
