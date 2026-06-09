namespace Soundtrail.Contracts.Commands;

public sealed record PlaybackReferenceLookupKeyDto(
    PlaybackReferenceLookupModeDto Mode,
    string? Isrc,
    string? Title,
    string? Artist);
