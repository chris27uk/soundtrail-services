using Raven.Client.Documents;
using Raven.Client.Documents.Queries;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.Search.Adapters;

public sealed class RavenSearchPort(IDocumentStore documentStore) : ISearchPort
{
    public async Task<SearchResponse?> SearchAsync(SearchCriteria searchCriteria, CancellationToken cancellationToken)
    {
        var activeSession = documentStore.OpenAsyncSession();
        var normalizedQuery = MusicIdentityText.NormalizeFreeText(searchCriteria.Query);
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return null;
        }

        IQueryable<CatalogSearchCandidateRecordDto> query = activeSession.Query<CatalogSearchCandidateRecordDto>()
            .Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(5)))
            .Search(x => x.SearchText, normalizedQuery, @operator: SearchOperator.And);

        if (searchCriteria.SearchTypes != SearchType.All)
        {
            query = query.Where(x => x.CandidateKind == searchCriteria.SearchTypes.ToString().ToLowerInvariant());
        }

        var existing = await query.Take(10).ToListAsync(cancellationToken);
        if (existing.Count == 0)
        {
            return null;
        }

        return new SearchResponse(
            searchCriteria.Query,
            searchCriteria.SearchTypes,
            existing.Select(
                    result => new SearchResultResponse(
                        ParseMusicCatalogId(result.CatalogItemId, result.CandidateKind),
                        ParseResultType(result.CandidateKind),
                        result.Title,
                        result.ArtistName,
                        result.AlbumTitle,
                        result.ArtworkUrl))
                .ToArray());
    }

    private static SearchType ParseResultType(string candidateKind) =>
        candidateKind switch
        {
            "artist" => SearchType.Artist,
            "album" => SearchType.Album,
            "track" => SearchType.Track,
            _ => throw new InvalidOperationException($"Unsupported candidate kind '{candidateKind}'.")
        };

    private static CatalogItemId ParseMusicCatalogId(string value, string candidateKind) =>
        candidateKind switch
        {
            "artist" => new CatalogItemId.Artist(ArtistId.From(value)),
            "album" => new CatalogItemId.Album(AlbumId.From(value)),
            "track" => new CatalogItemId.Track(TrackId.From(value)),
            _ => throw new InvalidOperationException($"Unsupported candidate kind '{candidateKind}'.")
        };
}
