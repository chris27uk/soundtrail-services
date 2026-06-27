namespace Soundtrail.Contracts.Api;

public sealed record SearchCatalogResponseDto(
    string Query,
    IReadOnlyList<SearchCatalogResultResponseDto> Results,
    SearchDiscoveryResponseDto Discovery);

public sealed record SearchCatalogResultResponseDto(
    string Type,
    string Id,
    string Name,
    string? ArtistId,
    string? ArtistName,
    string? AlbumId,
    string? AlbumName,
    string PlayabilityStatus,
    IReadOnlyList<string> AvailableProviders,
    IReadOnlyList<string> TerminallyUnavailableProviders,
    IReadOnlyList<ProviderReferenceResponseDto> ProviderReferences);

public sealed record SearchDiscoveryResponseDto(
    bool WillBeLookedUp,
    string? Reason,
    int? RetryAfterSeconds);
