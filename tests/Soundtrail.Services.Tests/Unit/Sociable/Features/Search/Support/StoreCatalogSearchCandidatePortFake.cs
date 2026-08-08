using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Adapters;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Search.Support;

internal sealed class StoreCatalogSearchCandidatePortFake : IStoreCatalogSearchCandidatePort
{
    private readonly Dictionary<string, CatalogSearchCandidateProjection> candidates = new(StringComparer.Ordinal);

    public IReadOnlyCollection<CatalogSearchCandidateProjection> Candidates => this.candidates.Values;

    public Task StoreAsync(CatalogSearchCandidateProjection projection, CancellationToken cancellationToken)
    {
        this.candidates[projection.CatalogItemId] = projection;
        return Task.CompletedTask;
    }

    public Task<SearchResponse?> SearchAsync(SearchCriteria searchCriteria, CancellationToken cancellationToken)
    {
        var query = StringNormalizationExtensions.Normalize(searchCriteria.Query);
        var matches = candidates.Values
            .Where(candidate =>
                candidate.CandidateKind is "artist" or "album" or "track")
            .Where(candidate =>
                searchCriteria.SearchTypes == SearchType.All ||
                string.Equals(
                    candidate.CandidateKind,
                    searchCriteria.SearchTypes.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            .Where(candidate => StringNormalizationExtensions.Normalize(candidate.SearchText) == query)
            .Take(10)
            .Select(static candidate => new SearchResultResponse(
                ParseMusicCatalogId(candidate.CatalogItemId, candidate.CandidateKind),
                ParseResultType(candidate.CandidateKind),
                candidate.Title,
                candidate.ArtistName,
                candidate.AlbumTitle,
                candidate.ArtworkUrl))
            .ToArray();

        return Task.FromResult<SearchResponse?>(
            matches.Length == 0
                ? null
                : new SearchResponse(searchCriteria.Query, searchCriteria.SearchTypes, matches));
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
