using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;
using Soundtrail.Domain.Catalog;
using Soundtrail.Translators.Registry;

namespace Soundtrail.Translators.ProjectionDocuments.Registrations;

public sealed class CatalogArtistProjectionToCatalogArtistRecordTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<CatalogArtistProjection, CatalogArtistRecordDto>(
            mapOnto: (projection, document) =>
            {
                document.ArtistId = projection.ArtistId;
                document.Name = projection.Name;
                document.NormalizedName = projection.NormalizedName;
                document.SearchText = MusicIdentityText.NormalizeFreeText(projection.Name);
                document.MusicBrainzArtistId = projection.MusicBrainzArtistId;
                document.AvailableProviders = projection.AvailableProviders.ToArray();
                document.TerminallyUnavailableProviders = projection.TerminallyUnavailableProviders.ToArray();
                document.ArtworkUrl = projection.ArtworkUrl;
                document.UpdatedAt = projection.UpdatedAt;
            });
    }
}
