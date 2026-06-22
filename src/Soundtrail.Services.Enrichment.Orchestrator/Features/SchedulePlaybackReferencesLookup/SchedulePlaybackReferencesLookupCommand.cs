using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.SchedulePlaybackReferencesLookup;

public sealed record SchedulePlaybackReferencesLookupCommand(
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset ObservedAt,
    CorrelationId CorrelationId,
    PlaybackReferenceSearchTermDto SearchTerm,
    string? ArtistId,
    string? AlbumId);
