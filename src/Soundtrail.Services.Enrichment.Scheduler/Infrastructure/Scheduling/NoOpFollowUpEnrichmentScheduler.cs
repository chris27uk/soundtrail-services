using Soundtrail.Services.Enrichment.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Scheduling;

public sealed class NoOpFollowUpEnrichmentScheduler : IFollowUpEnrichmentScheduler
{
    public Task ScheduleAsync(
        TrackEnrichmentState state,
        EnrichmentResponse response,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
