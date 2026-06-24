using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record StreamingLocationSearchTermDto(
    MusicSearchKind Kind,
    string? Query,
    string? Isrc,
    string? Title,
    string? Artist,
    string? Album);
