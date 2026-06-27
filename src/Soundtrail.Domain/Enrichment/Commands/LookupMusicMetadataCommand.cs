using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Enrichment.Commands;

public sealed record LookupTrackMetadataCommand(
    CommandId CommandId,
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    CorrelationId CorrelationId,
    MusicSearchCriteria SearchCriteria,
    CatalogTrackHierarchy? Hierarchy = null) : IMusicCatalogLookupCommand;
