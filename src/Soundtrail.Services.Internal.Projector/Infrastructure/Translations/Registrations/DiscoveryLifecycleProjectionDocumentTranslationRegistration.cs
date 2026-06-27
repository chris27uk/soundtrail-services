using Soundtrail.Contracts;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;
using Soundtrail.Translators.Discovery;
using Soundtrail.Translators.Registry;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Translations.Registrations;

public sealed class DiscoveryLifecycleProjectionDocumentTranslationRegistration : ITypeTranslationRegistration
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

        registry.Register<DiscoveryLifecycleProjection, DiscoveryLifecycleProjectionCheckpointDocument>(
            mapOnto: (projection, document) =>
            {
                document.Criteria = MusicSearchTermPersistentIdTranslator.ToPersistentId(projection.SearchCriteria);
                document.LastAppliedVersion = projection.ProjectionVersion;
                document.UpdatedAt = projection.UpdatedAt;
            });
    }
}
