using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;
using Soundtrail.Adapters.Discovery;
using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Search;

namespace Soundtrail.Adapters.ProjectionDocuments.Registrations;

public sealed class DiscoveryLifecycleProjectionToCheckpointTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<DiscoveryLifecycleProjection, DiscoveryLifecycleProjectionCheckpointDocument>(
            mapOnto: (projection, document) =>
            {
                document.Criteria = DiscoveryQueryKey.StableValueFor(projection.SearchCriteria);
                document.LastAppliedVersion = projection.ProjectionVersion;
                document.UpdatedAt = projection.UpdatedAt;
            });
    }
}
