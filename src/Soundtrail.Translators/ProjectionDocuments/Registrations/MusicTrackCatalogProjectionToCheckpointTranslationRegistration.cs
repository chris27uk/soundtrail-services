using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;
using Soundtrail.Translators.Registry;

namespace Soundtrail.Translators.ProjectionDocuments.Registrations;

public sealed class MusicTrackCatalogProjectionToCheckpointTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<MusicTrackCatalogProjection, CatalogProjectionCheckpointDocument>(
            mapOnto: (projection, document) =>
            {
                document.MusicCatalogId = projection.MusicCatalogId.Value;
                document.LastAppliedVersion = projection.ProjectionVersion;
                document.UpdatedAt = projection.Track.UpdatedAt;
            });
    }
}
