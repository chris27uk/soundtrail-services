namespace Soundtrail.Contracts.Commands;

public sealed record PlaybackReferenceSearchTermDto(
    string? Isrc,
    string? Title,
    string? Artist,
    string? Album);
