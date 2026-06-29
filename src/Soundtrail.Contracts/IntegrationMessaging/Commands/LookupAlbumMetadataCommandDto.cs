using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record LookupAlbumMetadataCommandDto(
    string CommandId,
    string ArtistId,
    string AlbumId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId,
    string ArtistName,
    string AlbumTitle,
    string? SourceAlbumId,
    string? SourceArtistId);
