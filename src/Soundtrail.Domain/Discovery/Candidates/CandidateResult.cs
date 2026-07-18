using Dunet;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery.Candidates
{
    [Union]
    public partial record CandidatesResult
    {
        public partial record Results(CandidateList CandidateList);

        partial record None;
    }

    public record ScoredCandidate(CatalogItemId Id, int Score);
}