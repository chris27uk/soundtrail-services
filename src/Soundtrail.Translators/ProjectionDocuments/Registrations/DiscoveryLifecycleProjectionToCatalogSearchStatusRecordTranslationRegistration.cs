using Soundtrail.Contracts;
using Soundtrail.Domain.Discovery;
using Soundtrail.Translators.Discovery;
using Soundtrail.Translators.Registry;

namespace Soundtrail.Translators.ProjectionDocuments.Registrations;

public sealed class DiscoveryLifecycleProjectionToCatalogSearchStatusRecordTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<DiscoveryLifecycleProjection, CatalogSearchStatusRecordDto>(
            mapOnto: (projection, document) =>
            {
                document.Criteria = MusicSearchTermPersistentIdTranslator.ToPersistentId(projection.SearchCriteria);
                document.Status = projection.Status;
                document.Priority = projection.Priority;
                document.WillBeLookedUp = projection.WillBeLookedUp;
                document.EstimatedRetryAfterSeconds = projection.EstimatedRetryAfterSeconds;
                document.EarliestExpectedCompletionAt = projection.EarliestExpectedCompletionAt;
                document.Reason = projection.Reason;
                document.UpdatedAt = projection.UpdatedAt;
            });
    }
}
