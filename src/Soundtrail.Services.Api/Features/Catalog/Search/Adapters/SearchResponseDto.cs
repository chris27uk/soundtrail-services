using Soundtrail.Services.Api.Shared.Adapters;

namespace Soundtrail.Services.Api.Features.Catalog.Search.Adapters;

public sealed record SearchResponseDto(
    string QueryText,
    string Filter,
    SearchResultResponseDto[] Results,
    DiscoveryFeedbackResponseDto? Discovery);

public sealed record SearchResultResponseDto(
    string MusicCatalogId,
    string ResultType,
    string Title,
    string? ArtistName,
    string? AlbumTitle,
    string? ArtworkUrl);
