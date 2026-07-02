namespace Soundtrail.Domain.Discovery;

public interface IDiscoveryBacklogPlanningReadPort
{
    Task<IReadOnlyList<DiscoveryBacklogCandidate>> GetPlanningCandidatesAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken);
}
