using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;

namespace Soundtrail.Contracts.IntegrationMessaging.Events;

public sealed record PlaybackReferencesResolutionRequiredMessageDto(
    string MusicCatalogId,
    LookupPriorityBand Priority,
    string CorrelationId,
    string SourceProvider,
    DateTimeOffset ObservedAt,
    StreamingLocationSearchTermDto SearchTerm,
    string? ArtistId,
    string? AlbumId);
