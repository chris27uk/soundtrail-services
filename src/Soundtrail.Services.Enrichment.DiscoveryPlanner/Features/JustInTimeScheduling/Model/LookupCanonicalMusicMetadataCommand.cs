using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model;

public sealed record LookupCanonicalMusicMetadataCommand(
    CommandId CommandId,
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    CorrelationId CorrelationId,
    CanonicalMusicMetadataLookup Lookup)
    : LookupPhaseCommand(CommandId, MusicCatalogId, Priority, CreatedAt, CorrelationId);
