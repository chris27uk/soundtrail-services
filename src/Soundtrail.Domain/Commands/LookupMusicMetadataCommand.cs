using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Commands;

public sealed record LookupMusicMetadataCommand(
    CommandId CommandId,
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    CorrelationId CorrelationId,
    MusicSearchTerm SearchTerm,
    CatalogTrackHierarchy? Hierarchy = null) : LookupPhaseCommand(CommandId, MusicCatalogId, Priority, CreatedAt, CorrelationId);
