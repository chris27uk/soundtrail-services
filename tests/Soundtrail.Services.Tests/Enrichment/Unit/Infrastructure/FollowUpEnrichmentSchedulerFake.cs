using Soundtrail.Services.Enrichment.Shared.Execution;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

public sealed class FollowUpEnrichmentSchedulerFake : IFollowUpEnrichmentScheduler
{
    public List<(TrackEnrichmentState State, EnrichmentResponse Response)> Scheduled { get; } = [];

    public Task ScheduleAsync(
        TrackEnrichmentState state,
        EnrichmentResponse response,
        CancellationToken cancellationToken)
    {
        Scheduled.Add((state, response));
        return Task.CompletedTask;
    }
}
