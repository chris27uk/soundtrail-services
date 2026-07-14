using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record LookupTrackMetadataCommandDto(
    string CommandId,
    string MusicCatalogId,
    LookupPriorityBandDto Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId,
    MusicSearchKindDto SearchKindDto,
    string? Query,
    string? Isrc,
    string? TrackName,
    string? ArtistName,
    string? AlbumName,
    string? ArtistId,
    string? AlbumId);
