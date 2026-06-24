using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnStreamingLocationsRequired.Support;

public sealed record ScheduleStreamingLocationsLookupCommand(
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset ObservedAt,
    CorrelationId CorrelationId,
    MusicSearchTerm SearchTerm,
    ArtistId? ArtistId,
    AlbumId? AlbumId);
