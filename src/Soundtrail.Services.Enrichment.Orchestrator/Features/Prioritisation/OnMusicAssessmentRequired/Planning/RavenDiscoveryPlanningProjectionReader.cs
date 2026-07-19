using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Assesment;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Planning;

public sealed class RavenDiscoveryPlanningProjectionReader(
    IAsyncDocumentSession session) : IDiscoveryPlanningProjectionReader
{
    private static readonly string[] RelevantEventTypes =
    [
        "work-scheduled",
        "work-deferred",
        "work-completed",
        "work-rejected",
        "work-ignored"
    ];

    public async Task<DiscoveryPlanningProjection> ReadAsync(
        EnrichmentTarget target,
        CancellationToken cancellationToken)
    {
        var query = session.Query<RavenStoredEventRecord>()
            .Where(x => x.AggregateType == "catalog-search" && RelevantEventTypes.Contains(x.EventType))
            .OrderBy(x => x.StreamId)
            .ThenBy(x => x.Version);

        var records = await query.ToListAsync(cancellationToken);

        var equivalentStreamId = CatalogWorkId.From(target).StableValue;
        var activeWorkCount = 0;
        var activeHighPriorityWorkCount = 0;
        var hasEquivalentWorkInFlight = false;
        DateTimeOffset? equivalentWorkExpectedCompletionAt = null;

        foreach (var stream in records.GroupBy(x => x.StreamId))
        {
            var state = Project(stream.OrderBy(x => x.Version));
            if (!state.IsActive)
            {
                continue;
            }

            activeWorkCount++;
            if (state.Priority == LookupPriorityBand.High)
            {
                activeHighPriorityWorkCount++;
            }

            if (stream.Key == equivalentStreamId)
            {
                hasEquivalentWorkInFlight = true;
                equivalentWorkExpectedCompletionAt = state.EarliestExpectedCompletionAt;
            }
        }

        return new DiscoveryPlanningProjection(
            hasEquivalentWorkInFlight,
            equivalentWorkExpectedCompletionAt,
            activeWorkCount,
            activeHighPriorityWorkCount);
    }

    private static ProjectedPlanningState Project(IEnumerable<RavenStoredEventRecord> events)
    {
        var isActive = false;
        LookupPriorityBand priority = LookupPriorityBand.Low;
        DateTimeOffset? earliestExpectedCompletionAt = null;

        foreach (var @event in events)
        {
            switch (@event.EventType)
            {
                case "work-scheduled":
                    isActive = true;
                    if (@event.Body is CatalogDiscoveryWorkScheduledEventDataRecordDto scheduled &&
                        Enum.TryParse<LookupPriorityBand>(scheduled.Priority, true, out var scheduledPriority))
                    {
                        priority = scheduledPriority;
                        earliestExpectedCompletionAt = scheduled.EarliestExpectedCompletionAt;
                    }
                    break;

                case "work-deferred":
                case "work-completed":
                case "work-rejected":
                    isActive = false;
                    break;

                case "work-ignored":
                    if (@event.Body is CatalogDiscoveryWorkIgnoredEventDataRecordDto ignored)
                    {
                        earliestExpectedCompletionAt = ignored.EarliestExpectedCompletionAt;
                        if (Enum.TryParse<LookupPriorityBand>(ignored.Priority, true, out var ignoredPriority))
                        {
                            priority = ignoredPriority;
                        }
                    }
                    break;
            }
        }

        return new ProjectedPlanningState(isActive, priority, earliestExpectedCompletionAt);
    }

    private sealed record ProjectedPlanningState(
        bool IsActive,
        LookupPriorityBand Priority,
        DateTimeOffset? EarliestExpectedCompletionAt);
}
