using System.Collections;

namespace Soundtrail.Domain.Discovery.Candidates;

public sealed class CandidateList : IEnumerable<ScoredCandidate>
{
    private readonly SortedSet<ScoredCandidate> candidates;

    private CandidateList(IEnumerable<ScoredCandidate> candidates)
    {
        this.candidates = new SortedSet<ScoredCandidate>(candidates);
    }

    public bool Any() => candidates.Count > 0;

    public static CandidateList From(List<ScoredCandidate> candidates) => new CandidateList(candidates);
    
    public static CandidateList From(CandidatesResult result)
    {
        var allCandidates = result switch
        {
            CandidatesResult.Results found => found.Value
                .OrderByDescending(candidate => candidate.Score)
                .ToArray(),
            CandidatesResult.None => [],
            _ => throw new InvalidOperationException($"Unsupported candidate result '{result.GetType().Name}'.")
        };

        if (allCandidates.Length == 0)
        {
            return new CandidateList([]);
        }

        var topScore = allCandidates[0].Score;
        var threshold = (int)Math.Ceiling(topScore * 0.85m);
        var qualified = allCandidates
            .Where(candidate => candidate.Score >= threshold)
            .ToArray();

        return new CandidateList(qualified);
    }

    public IEnumerator<ScoredCandidate> GetEnumerator() => this.candidates.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
