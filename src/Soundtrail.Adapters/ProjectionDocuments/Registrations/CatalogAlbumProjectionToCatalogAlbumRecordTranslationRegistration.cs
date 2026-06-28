using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;
using Soundtrail.Domain.Catalog;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.ProjectionDocuments.Registrations;

public sealed class CatalogAlbumProjectionToCatalogAlbumRecordTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<CatalogAlbumProjection, CatalogAlbumRecordDto>(
            mapOnto: (projection, document) =>
            {
                document.AlbumId = projection.AlbumId;
                document.ArtistId = projection.ArtistId;
                document.Name = projection.Name;
                document.NormalizedName = projection.NormalizedName;
                document.ArtistName = projection.ArtistName;
                document.SearchText = MusicIdentityText.NormalizeFreeText($"{projection.Name} {projection.ArtistName}".Trim());
                document.MusicBrainzReleaseId = projection.MusicBrainzReleaseId;
                document.AvailableProviders = projection.AvailableProviders.ToArray();
                document.TerminallyUnavailableProviders = projection.TerminallyUnavailableProviders.ToArray();
                document.ArtworkUrl = projection.ArtworkUrl;
                document.ReleaseDate = projection.ReleaseDate;
                document.UpdatedAt = projection.UpdatedAt;
            });
    }
}
