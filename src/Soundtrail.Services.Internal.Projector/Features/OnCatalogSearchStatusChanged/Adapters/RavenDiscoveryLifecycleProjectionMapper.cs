using Soundtrail.Contracts;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Translators.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;

public sealed class RavenDiscoveryLifecycleProjectionMapper
{
    public DiscoveryLifecycleProjection ToDomain(
        MusicSearchCriteria searchCriteria,
        CatalogSearchStatusRecordDto? status,
        DiscoveryLifecycleProjectionCheckpointDocument? checkpoint)
    {
        var snapshot = new DiscoveryLifecycleProjectionSnapshot(
            searchCriteria,
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
            Id = CatalogSearchStatusRecordDto.GetDocumentId(MusicSearchTermPersistentIdTranslator.ToPersistentId(projection.SearchCriteria)),
            Criteria = MusicSearchTermPersistentIdTranslator.ToPersistentId(projection.SearchCriteria),
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
            Id = DiscoveryLifecycleProjectionCheckpointDocument.GetDocumentId(MusicSearchTermPersistentIdTranslator.ToPersistentId(projection.SearchCriteria)),
            Criteria = MusicSearchTermPersistentIdTranslator.ToPersistentId(projection.SearchCriteria),
            LastAppliedVersion = projection.ProjectionVersion,
            UpdatedAt = projection.UpdatedAt
        };

    public void MapOntoStatusDocument(
        CatalogSearchStatusRecordDto document,
        DiscoveryLifecycleProjection projection)
    {
        document.Criteria = MusicSearchTermPersistentIdTranslator.ToPersistentId(projection.SearchCriteria);
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
        document.Criteria = MusicSearchTermPersistentIdTranslator.ToPersistentId(projection.SearchCriteria);
        document.LastAppliedVersion = projection.ProjectionVersion;
        document.UpdatedAt = projection.UpdatedAt;
    }
}
