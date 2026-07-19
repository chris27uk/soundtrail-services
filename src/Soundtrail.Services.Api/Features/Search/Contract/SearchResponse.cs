using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Api.Features.Search.Contract;

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

public sealed record DiscoveryFeedbackResponse(
    string Status,
    LookupPriorityBand Priority,
    DateTimeOffset? NextEligibleAt,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason,
    DateTimeOffset UpdatedAtUtc);
