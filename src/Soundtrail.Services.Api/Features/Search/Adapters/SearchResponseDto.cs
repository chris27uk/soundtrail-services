namespace Soundtrail.Services.Api.Features.Search.Registrations;

public sealed record SearchResponseDto(
    string QueryText,
    string Filter,
    SearchResultResponseDto[] Results);

public sealed record SearchResultResponseDto(
    string MusicCatalogId,
    string ResultType,
    string Title,
    string? ArtistName,
    string? AlbumTitle,
    string? ArtworkUrl);
