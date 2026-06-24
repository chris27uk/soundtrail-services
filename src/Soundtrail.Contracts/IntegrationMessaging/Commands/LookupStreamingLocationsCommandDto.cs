using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record LookupStreamingLocationsCommandDto(
    string CommandId,
    string MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId,
    StreamingLocationSearchTermDto SearchTerm,
    string? ArtistId,
    string? AlbumId);
