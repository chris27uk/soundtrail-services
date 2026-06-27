using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.ProjectionDocuments.Registrations;

public sealed class CatalogTrackProjectionToCatalogTrackRecordTranslationRegistration : ITypeTranslationRegistration
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
    }
}
