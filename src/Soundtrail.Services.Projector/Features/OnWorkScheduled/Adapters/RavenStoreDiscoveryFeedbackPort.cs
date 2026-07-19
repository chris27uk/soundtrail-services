using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkScheduled.Adapters;

public sealed class RavenStoreDiscoveryFeedbackPort(
    IDocumentStore documentStore) : IStoreDiscoveryFeedbackPort
{
    public async Task StoreAsync(WorkScheduled @event, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var record = new CatalogDiscoveryFeedbackRecordDto
        {
            Id = CatalogDiscoveryFeedbackRecordDto.GetDocumentId(@event.Target.NormalisedIdentifier),
            TargetId = @event.Target.NormalisedIdentifier,
            Status = "scheduled",
            Priority = @event.Priority.ToString(),
            NextEligibleAtUtc = @event.NextEligibleAt,
            EarliestExpectedCompletionAtUtc = @event.EarliestExpectedCompletionAt,
            Reason = @event.Reason,
            UpdatedAtUtc = @event.ScheduledAt
        };

        await session.StoreAsync(record, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
    }
}
