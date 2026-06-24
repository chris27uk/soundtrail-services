using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record LookupMusicMetadataCommandDto(
    string CommandId,
    string MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId,
    MusicSearchKind SearchKind,
    string? Query,
    string? Isrc,
    string? TrackName,
    string? ArtistName,
    string? AlbumName,
    string? ArtistId,
    string? AlbumId);
