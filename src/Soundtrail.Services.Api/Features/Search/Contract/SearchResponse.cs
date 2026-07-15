using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Api.Features.Search.Contract;

public sealed record SearchResponse(
    string QueryText,
    SearchFilter Filter,
    SearchResultResponse[] Results);

public sealed record SearchResultResponse(
    CatalogItemId MusicCatalogId,
    SearchFilter ResultType,
    string Title,
    string? ArtistName,
    string? AlbumTitle,
    string? ArtworkUrl);
