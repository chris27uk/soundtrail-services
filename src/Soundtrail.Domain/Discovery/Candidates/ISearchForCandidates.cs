using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Candidates
{
    public interface ISearchForCandidates
    {
        CandidateResults Search(EnrichmentQuery query);
    }
}
