using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record LookupAlbumMetadataCommandDto(
    string CommandId,
    string ArtistId,
    string AlbumId,
    LookupPriorityBandDto Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId,
    string ArtistName,
    string AlbumTitle,
    string? SourceAlbumId,
    string? SourceArtistId);
