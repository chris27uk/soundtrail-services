using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class DiscoveryBacklogPlanningReadPortFake : IDiscoveryBacklogPlanningReadPort
{
    private readonly List<DiscoveryBacklogCandidate> candidates = [];

    public Task<IReadOnlyList<DiscoveryBacklogCandidate>> GetPlanningCandidatesAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<DiscoveryBacklogCandidate> eligible = candidates
            .Where(candidate => candidate.EarliestExpectedCompletionAt is null || candidate.EarliestExpectedCompletionAt <= now)
            .OrderBy(candidate => candidate.EarliestExpectedCompletionAt)
            .ThenBy(candidate => candidate.UpdatedAt)
            .Take(take)
            .ToArray();

        return Task.FromResult(eligible);
    }

    public void Seed(params DiscoveryBacklogCandidate[] items)
    {
        candidates.AddRange(items);
    }
}
