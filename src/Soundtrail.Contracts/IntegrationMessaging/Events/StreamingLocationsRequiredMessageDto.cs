using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;

namespace Soundtrail.Contracts.IntegrationMessaging.Events;

public sealed record StreamingLocationsRequiredMessageDto(
    string MusicCatalogId,
    LookupPriorityBandDto Priority,
    string CorrelationId,
    string SourceProvider,
    DateTimeOffset ObservedAt,
    StreamingLocationSearchTermDto SearchTerm,
    string? ArtistId,
    string? AlbumId);
