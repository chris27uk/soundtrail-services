using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Candidates
{
    public interface ISearchForCandidates
    {
        CandidatesResult Search(EnrichmentTarget target);
    }
}
