using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Candidates;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes
{
    internal sealed class SearchForCandidatesFake(StoreCatalogSearchCandidatePortFake searchCandidates) : ISearchForCandidates
    {
        public CandidatesResult Search(EnrichmentTarget target)
        {
            if (target is not EnrichmentTarget.SearchForUnknownCatalogItem(var searchCriteria))
            {
                return new CandidatesResult.None();
            }

            var normalized = searchCriteria.NormalisedIdentifier["search:".Length..];
            var matches = searchCandidates.Candidates
                .Where(candidate => candidate.CandidateKind == "track")
                .Where(candidate => StringNormalizationExtensions.Normalize(candidate.SearchText) == normalized)
                .Select(candidate => new ScoredCandidate(new CatalogItemId.Track(TrackId.From(candidate.CatalogItemId)), 100))
                .ToList();
            return matches.Count == 0
                ? new CandidatesResult.None()
                : new CandidatesResult.Results(CandidateList.From(matches));
        }
    }
}
