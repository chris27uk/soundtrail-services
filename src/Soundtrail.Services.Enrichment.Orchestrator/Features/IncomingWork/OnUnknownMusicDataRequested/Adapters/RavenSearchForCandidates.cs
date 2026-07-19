using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Candidates;
using Soundtrail.Services.Api.Features.Search.Contract;
using System.Globalization;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnUnknownMusicDataRequested.Adapters;

public sealed class RavenSearchForCandidates(IDocumentStore documentStore) : ISearchForCandidates
{
    private const string Album = "album";
    private const string Artist = "artist";
    private const string Track = "track";
    
    public CandidatesResult Search(EnrichmentTarget target)
    {
        if (target is not EnrichmentTarget.SearchForUnknownCatalogItem(var searchCriteria))
        {
            return new CandidatesResult.None();
        }

        using var session = documentStore.OpenSession();
        IQueryable<CatalogSearchCandidateRecordDto> query = session.Query<CatalogSearchCandidateRecordDto>()
            .Search(x => x.SearchText, searchCriteria.Query);

        if (searchCriteria.SearchTypes != SearchType.All)
        {
            query = query.Where(x => x.CandidateKind == searchCriteria.SearchTypes.ToString());
        }

        var matches = query
            .Take(10)
            .ToList()
            .Select(result => new ScoredCandidate(ParseCatalogItemId(result.CatalogItemId, result.CandidateKind), ReadScore(session, result)))
            .ToList();

        if (matches.Count == 0)
        {
            return new CandidatesResult.None();
        }

        return new CandidatesResult.Results(CandidateList.From(matches));
    }

    private static int ReadScore(IDocumentSession session, CatalogSearchCandidateRecordDto result)
    {
        var metadata = session.Advanced.GetMetadataFor(result);
        if (!metadata.TryGetValue("@index-score", out var rawScore))
        {
            return 0;
        }

        var text = Convert.ToString(rawScore, CultureInfo.InvariantCulture);
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? (int)Math.Round(parsed * 1000d)
            : 0;
    }

    private static CatalogItemId ParseCatalogItemId(string value, string candidateKind) =>
        candidateKind switch
        {
            Track => new CatalogItemId.Track(TrackId.From(value)),
            Album => new CatalogItemId.Album(AlbumId.From(value)),
            Artist => new CatalogItemId.Artist(ArtistId.From(value)),
            _ => throw new InvalidOperationException($"Unsupported candidate kind '{candidateKind}'.")
        };
}
