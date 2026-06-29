using Soundtrail.Contracts;
using Soundtrail.Domain.Discovery;
using Soundtrail.Adapters.Discovery;
using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Search;

namespace Soundtrail.Adapters.ProjectionDocuments.Registrations;

public sealed class DiscoveryLifecycleProjectionToCatalogSearchStatusRecordTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<DiscoveryLifecycleProjection, CatalogSearchStatusRecordDto>(
            mapOnto: (projection, document) =>
            {
                document.Criteria = DiscoveryQueryKey.StableValueFor(projection.SearchCriteria);
                document.Status = projection.Status;
                document.Priority = projection.Priority;
                document.MusicCatalogId = projection.MusicCatalogId?.Value;
                document.WillBeLookedUp = projection.WillBeLookedUp;
                document.EstimatedRetryAfterSeconds = projection.EstimatedRetryAfterSeconds;
                document.EarliestExpectedCompletionAt = projection.EarliestExpectedCompletionAt;
                document.Reason = projection.Reason;
                document.UpdatedAt = projection.UpdatedAt;
            });
    }
}
