namespace Soundtrail.Services.Enrichment.Shared.Execution;

public interface IFollowUpEnrichmentScheduler
{
    Task ScheduleAsync(
        TrackEnrichmentState state,
        EnrichmentResponse response,
        CancellationToken cancellationToken);
}
