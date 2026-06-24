using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnStreamingLocationsRequired.Support;

public sealed record ScheduleStreamingLocationsLookupCommand(
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset ObservedAt,
    CorrelationId CorrelationId,
    PlaybackReferenceSearchTermDto SearchTerm,
    string? ArtistId,
    string? AlbumId);
