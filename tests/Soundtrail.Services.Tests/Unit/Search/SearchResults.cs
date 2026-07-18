using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Search.Contract;

namespace Soundtrail.Services.Tests.Unit.Search;

internal static class SearchResults
{
    public static SearchResponse CreateResponse(
        string queryText = "u2",
        SearchType filter = SearchType.Artist,
        CatalogItemId? musicCatalogId = null,
        SearchType? resultType = null,
        string title = "U2",
        string? artistName = null,
        string? albumTitle = null,
        string? artworkUrl = "https://cdn.soundtrail.test/artists/artist-2901.jpg") =>
        new(
            queryText,
            filter,
            [
                new SearchResultResponse(
                    musicCatalogId ?? new CatalogItemId.Artist(ArtistId.From("artist-2901")),
                    resultType ?? filter,
                    title,
                    artistName,
                    albumTitle,
                    artworkUrl)
            ]);
}
