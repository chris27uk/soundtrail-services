using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record LookupArtistMetadataCommandDto(
    string CommandId,
    string ArtistId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId,
    string ArtistName,
    string? SourceArtistId);
