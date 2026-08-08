using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Candidates;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Search;

internal sealed class SearchForCandidatesFake : ISearchForCandidates
{
    private readonly StoreCatalogSearchCandidatePortFake? searchCandidates;

    public SearchForCandidatesFake()
    {
    }

    public SearchForCandidatesFake(StoreCatalogSearchCandidatePortFake searchCandidates) =>
        this.searchCandidates = searchCandidates;

    public int Calls { get; private set; }

    public EnrichmentTarget? LastTarget { get; private set; }

    public CandidatesResult? ResultToReturn { get; set; }

    public CandidatesResult Search(EnrichmentTarget target)
    {
        Calls++;
        LastTarget = target;

        if (ResultToReturn is not null || searchCandidates is null)
        {
            return ResultToReturn ?? new CandidatesResult.None();
        }

        if (target is not EnrichmentTarget.SearchForUnknownCatalogItem(var searchCriteria))
        {
            return new CandidatesResult.None();
        }

        var normalized = StringNormalizationExtensions.Normalize(searchCriteria.Query);
        var matches = searchCandidates.Candidates
            .Where(candidate =>
                searchCriteria.SearchTypes == SearchType.All ||
                string.Equals(
                    candidate.CandidateKind,
                    searchCriteria.SearchTypes.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            .Where(candidate => StringNormalizationExtensions.Normalize(candidate.SearchText) == normalized)
            .Take(1)
            .Select(candidate => new ScoredCandidate(ParseCatalogItemId(candidate), 100))
            .ToList();

        return matches.Count == 0
            ? new CandidatesResult.None()
            : new CandidatesResult.Results(CandidateList.From(matches));
    }

    private static CatalogItemId ParseCatalogItemId(CatalogSearchCandidateProjection candidate) =>
        candidate.CandidateKind.ToLowerInvariant() switch
        {
            "track" => new CatalogItemId.Track(TrackId.From(candidate.CatalogItemId)),
            "album" => new CatalogItemId.Album(AlbumId.From(candidate.CatalogItemId)),
            "artist" => new CatalogItemId.Artist(ArtistId.From(candidate.CatalogItemId)),
            "playlist" => new CatalogItemId.Playlist(PlaylistId.FromPlaylistName(candidate.CatalogItemId)),
            _ => throw new InvalidOperationException($"Unsupported candidate kind '{candidate.CandidateKind}'.")
        };
}
