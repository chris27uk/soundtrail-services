using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;

namespace Soundtrail.Adapters.ProjectionDocuments;

public sealed class RavenDiscoveryLifecycleProjectionMapper
{
    public DiscoveryLifecycleProjection ToDomain(
        LookupCriteria searchCriteria,
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
            status?.MusicCatalogId is null ? null : MusicCatalogId.From(status.MusicCatalogId),
            status?.UpdatedAt ?? default,
            checkpoint?.LastAppliedVersion ?? 0);

        return DiscoveryLifecycleProjection.Load(snapshot);
    }
}
