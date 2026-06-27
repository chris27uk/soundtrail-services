using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;
using Soundtrail.Translators.Registry;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Translations.Registrations;

public sealed class MusicTrackCatalogProjectionDocumentTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<CatalogTrackProjection, CatalogTrackRecordDto>(
            mapOnto: (projection, document) =>
            {
                document.TrackId = projection.TrackId;
                document.ArtistId = projection.ArtistId;
                document.AlbumId = projection.AlbumId;
                document.Title = projection.Title;
                document.NormalizedTitle = projection.NormalizedTitle;
                document.ArtistName = projection.ArtistName;
                document.AlbumName = projection.AlbumName;
                document.SearchText = projection.SearchText;
                document.MusicBrainzRecordingId = projection.MusicBrainzRecordingId;
                document.Isrc = projection.Isrc;
                document.DurationMs = projection.DurationMs;
                document.AvailableProviders = projection.AvailableProviders.ToArray();
                document.TerminallyUnavailableProviders = projection.TerminallyUnavailableProviders.ToArray();
                document.ProviderReferences = projection.ProviderReferences.Select(x => new CatalogProviderReferenceRecordDto
                {
                    Provider = x.Provider,
                    ProviderEntityType = x.ProviderEntityType,
                    ProviderId = x.ProviderId,
                    Url = x.Url,
                    DiscoveredAt = x.DiscoveredAt
                }).ToArray();
                document.ArtworkUrl = projection.ArtworkUrl;
                document.UpdatedAt = projection.UpdatedAt;
            });

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

        registry.Register<MusicTrackCatalogProjection, CatalogProjectionCheckpointDocument>(
            mapOnto: (projection, document) =>
            {
                document.MusicCatalogId = projection.MusicCatalogId.Value;
                document.LastAppliedVersion = projection.ProjectionVersion;
                document.UpdatedAt = projection.Track.UpdatedAt;
            });
    }
}
