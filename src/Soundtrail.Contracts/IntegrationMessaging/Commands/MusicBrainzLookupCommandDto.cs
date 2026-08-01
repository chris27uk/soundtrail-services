using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record MusicBrainzLookupCommandDto(
    string CommandId,
    string CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBandDto Priority,
    string LookupKind,
    string? Query,
    int SearchTypes,
    string? ArtistId,
    string? AlbumId);
