using Soundtrail.Contracts;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.ProjectDiscoveryLifecycle.Adapters;

public sealed class RavenDiscoveryLifecycleProjectionMapper
{
    public DiscoveryLifecycleProjection ToDomain(
        CatalogSearchCriteria criteria,
        CatalogSearchStatusRecordDto? status,
        DiscoveryLifecycleProjectionCheckpointDocument? checkpoint)
    {
        var snapshot = new DiscoveryLifecycleProjectionSnapshot(
            criteria,
            status?.Status ?? string.Empty,
            status?.Priority ?? string.Empty,
            status?.WillBeLookedUp ?? false,
            status?.EstimatedRetryAfterSeconds,
            status?.EarliestExpectedCompletionAt,
            status?.Reason,
            status?.UpdatedAt ?? default,
            checkpoint?.LastAppliedVersion ?? 0);

        return DiscoveryLifecycleProjection.Load(snapshot);
    }

    public CatalogSearchStatusRecordDto ToStatusDocument(DiscoveryLifecycleProjection projection) =>
        new()
        {
            Id = CatalogSearchStatusRecordDto.GetDocumentId(projection.Criteria.Value),
            Criteria = projection.Criteria.Value,
            Status = projection.Status,
            Priority = projection.Priority,
            WillBeLookedUp = projection.WillBeLookedUp,
            EstimatedRetryAfterSeconds = projection.EstimatedRetryAfterSeconds,
            EarliestExpectedCompletionAt = projection.EarliestExpectedCompletionAt,
            Reason = projection.Reason,
            UpdatedAt = projection.UpdatedAt
        };

    public DiscoveryLifecycleProjectionCheckpointDocument ToCheckpointDocument(DiscoveryLifecycleProjection projection) =>
        new()
        {
            Id = DiscoveryLifecycleProjectionCheckpointDocument.GetDocumentId(projection.Criteria.Value),
            Criteria = projection.Criteria.Value,
            LastAppliedVersion = projection.ProjectionVersion,
            UpdatedAt = projection.UpdatedAt
        };

    public void MapOntoStatusDocument(
        CatalogSearchStatusRecordDto document,
        DiscoveryLifecycleProjection projection)
    {
        document.Criteria = projection.Criteria.Value;
        document.Status = projection.Status;
        document.Priority = projection.Priority;
        document.WillBeLookedUp = projection.WillBeLookedUp;
        document.EstimatedRetryAfterSeconds = projection.EstimatedRetryAfterSeconds;
        document.EarliestExpectedCompletionAt = projection.EarliestExpectedCompletionAt;
        document.Reason = projection.Reason;
        document.UpdatedAt = projection.UpdatedAt;
    }

    public void MapOntoCheckpointDocument(
        DiscoveryLifecycleProjectionCheckpointDocument document,
        DiscoveryLifecycleProjection projection)
    {
        document.Criteria = projection.Criteria.Value;
        document.LastAppliedVersion = projection.ProjectionVersion;
        document.UpdatedAt = projection.UpdatedAt;
    }
}
