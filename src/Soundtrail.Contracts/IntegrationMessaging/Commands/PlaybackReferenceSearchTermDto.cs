namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record PlaybackReferenceSearchTermDto(
    string? Isrc,
    string? Title,
    string? Artist,
    string? Album);
