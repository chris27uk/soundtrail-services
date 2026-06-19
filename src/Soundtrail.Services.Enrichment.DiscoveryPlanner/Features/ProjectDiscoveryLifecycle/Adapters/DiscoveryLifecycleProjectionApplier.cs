using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle.Support;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle.Adapters;

public sealed class DiscoveryLifecycleProjectionApplier(
    DiscoveryLifecycleProjectionMutationService mutationService)
{
    public async Task ApplyStoredEventAsync(
        DiscoveryQueryStoredEventRecordDto storedEvent,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken)
    {
        var checkpoint = await session.LoadAsync<DiscoveryLifecycleProjectionCheckpointDocument>(
                             DiscoveryLifecycleProjectionCheckpointDocument.GetDocumentId(storedEvent.Criteria),
                             cancellationToken)
                         ?? new DiscoveryLifecycleProjectionCheckpointDocument
                         {
                             Id = DiscoveryLifecycleProjectionCheckpointDocument.GetDocumentId(storedEvent.Criteria),
                             Criteria = storedEvent.Criteria
                         };

        if (checkpoint.LastAppliedVersion >= storedEvent.Version)
        {
            return;
        }

        var status = await session.LoadAsync<CatalogSearchStatusRecordDto>(
                         CatalogSearchStatusRecordDto.GetDocumentId(storedEvent.Criteria),
                         cancellationToken)
                     ?? new CatalogSearchStatusRecordDto
                     {
                         Id = CatalogSearchStatusRecordDto.GetDocumentId(storedEvent.Criteria),
                         Criteria = storedEvent.Criteria
                     };

        mutationService.ApplyStoredEvent(storedEvent, status);

        checkpoint.LastAppliedVersion = storedEvent.Version;
        checkpoint.UpdatedAt = storedEvent.OccurredAtUtc;
        await session.StoreAsync(checkpoint, cancellationToken);
        await session.StoreAsync(status, cancellationToken);
    }
}
