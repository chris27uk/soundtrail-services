using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;
using Soundtrail.Translators.Discovery;
using Soundtrail.Translators.Registry;

namespace Soundtrail.Translators.ProjectionDocuments.Registrations;

public sealed class DiscoveryLifecycleProjectionToCheckpointTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<DiscoveryLifecycleProjection, DiscoveryLifecycleProjectionCheckpointDocument>(
            mapOnto: (projection, document) =>
            {
                document.Criteria = MusicSearchTermPersistentIdTranslator.ToPersistentId(projection.SearchCriteria);
                document.LastAppliedVersion = projection.ProjectionVersion;
                document.UpdatedAt = projection.UpdatedAt;
            });
    }
}
