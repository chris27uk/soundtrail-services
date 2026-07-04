using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;
using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Catalog.CatalogProjection;

namespace Soundtrail.Adapters.ProjectionDocuments.Registrations;

public sealed class MusicTrackCatalogProjectionToCheckpointTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<MusicTrackCatalogProjection, CatalogProjectionCheckpointDocument>(
            mapOnto: (projection, document) =>
            {
                document.ArtistId = projection.Artist?.ArtistId ?? string.Empty;
                document.LastAppliedVersion = projection.ProjectionVersion;
                document.UpdatedAt = projection.Track.UpdatedAt;
            });
    }
}
