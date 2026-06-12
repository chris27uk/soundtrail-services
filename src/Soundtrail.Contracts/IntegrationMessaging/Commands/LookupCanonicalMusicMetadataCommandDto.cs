using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record LookupCanonicalMusicMetadataCommandDto(
    string CommandId,
    string MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId,
    string? Isrc,
    string? TrackName,
    string? ArtistName,
    string? AlbumName);
