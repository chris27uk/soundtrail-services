namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record StreamingLocationSearchTermDto(
    string? Isrc,
    string? Title,
    string? Artist,
    string? Album);
