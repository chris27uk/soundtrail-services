using Dunet;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery.Candidates
{
    [Union]
    public partial record CandidateResults
    {
        public partial record CandidateFound(List<CandidateFound> Value);

        partial record None;
    }

    public record CandidateFound(CatalogItemId Value, int Score);
}