namespace Soundtrail.Contracts.Api;

public sealed record ArtistDetailsResponseDto(
    string Id,
    string Name,
    IReadOnlyList<AlbumSummaryResponseDto> Albums);

public sealed record ArtistTracksResponseDto(
    string ArtistId,
    string ArtistName,
    IReadOnlyList<TrackSummaryResponseDto> Tracks);

public sealed record AlbumDetailsResponseDto(
    string ArtistId,
    string ArtistName,
    string Id,
    string Name,
    DateOnly? ReleaseDate,
    IReadOnlyList<TrackSummaryResponseDto> Tracks);

public sealed record AlbumTracksResponseDto(
    string ArtistId,
    string ArtistName,
    string AlbumId,
    string AlbumName,
    IReadOnlyList<TrackSummaryResponseDto> Tracks);

public sealed record TrackDetailsResponseDto(
    string ArtistId,
    string ArtistName,
    string AlbumId,
    string AlbumName,
    string Id,
    string Title,
    string? Isrc,
    int? DurationMs,
    string PlayabilityStatus,
    IReadOnlyList<string> AvailableProviders,
    IReadOnlyList<string> TerminallyUnavailableProviders,
    IReadOnlyList<ProviderReferenceResponseDto> ProviderReferences);

public sealed record AlbumSummaryResponseDto(
    string Id,
    string Name,
    DateOnly? ReleaseDate,
    string PlayabilityStatus,
    IReadOnlyList<string> AvailableProviders,
    IReadOnlyList<string> TerminallyUnavailableProviders);

public sealed record TrackSummaryResponseDto(
    string Id,
    string Title,
    string AlbumId,
    string AlbumName,
    string? Isrc,
    int? DurationMs,
    string PlayabilityStatus,
    IReadOnlyList<string> AvailableProviders,
    IReadOnlyList<string> TerminallyUnavailableProviders,
    IReadOnlyList<ProviderReferenceResponseDto> ProviderReferences);
