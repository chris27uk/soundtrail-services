using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record StreamingLocationSearchTermDto(
    MusicSearchKindDto KindDto,
    string? Query,
    string? Isrc,
    string? Title,
    string? Artist,
    string? Album);
