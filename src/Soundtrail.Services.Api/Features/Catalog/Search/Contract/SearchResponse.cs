using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.Search.Contract;

public sealed record SearchResponse(
    string QueryText,
    SearchType Filter,
    SearchResultResponse[] Results,
    DiscoveryFeedbackResponse? Discovery = null);

public sealed record SearchResultResponse(
    CatalogItemId MusicCatalogId,
    SearchType ResultType,
    string Title,
    string? ArtistName,
    string? AlbumTitle,
    string? ArtworkUrl);
