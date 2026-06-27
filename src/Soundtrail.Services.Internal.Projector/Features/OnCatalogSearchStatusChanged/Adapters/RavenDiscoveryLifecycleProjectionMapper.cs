using Soundtrail.Contracts;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
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
}
