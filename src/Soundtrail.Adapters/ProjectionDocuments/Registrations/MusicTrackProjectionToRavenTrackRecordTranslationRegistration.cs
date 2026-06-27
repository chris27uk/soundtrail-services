using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.ProjectionDocuments.Registrations;

public sealed class MusicTrackProjectionToRavenTrackRecordTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<MusicTrackProjection, RavenTrackRecordDto>(
            mapOnto: (projection, document) =>
            {
                var snapshot = projection.ToSnapshot();
                document.ArtistId = snapshot.ArtistId;
                document.AlbumId = snapshot.AlbumId;
                document.Title = snapshot.Title;
                document.Artist = snapshot.Artist.Value;
                document.NormalizedArtist = snapshot.Artist.Normalized;
                document.AlbumTitle = snapshot.AlbumTitle.HasValue ? snapshot.AlbumTitle.Value : null;
                document.NormalizedAlbumTitle = snapshot.AlbumTitle.Normalized;
                document.SearchText = snapshot.SearchText;
                document.Isrc = snapshot.Isrc;
                document.NormalizedIsrc = snapshot.NormalizedIsrc;
                document.Mbid = snapshot.Mbid;
                document.NormalizedMbid = snapshot.NormalizedMbid;
                document.AppleId = snapshot.AppleId;
                document.SpotifyId = snapshot.SpotifyId;
                document.DurationMs = snapshot.DurationMs;
                document.ReleaseDate = snapshot.ReleaseDate;
                document.ArtworkUrl = snapshot.ArtworkUrl;
                document.ResolvedMetadata = snapshot.ResolvedMetadata is null
                    ? null
                    : new RavenSongMetadataRecordDto
                    {
                        Title = snapshot.ResolvedMetadata.Title,
                        Artist = snapshot.ResolvedMetadata.Artist.Value,
                        Isrc = snapshot.ResolvedMetadata.Isrc,
                        Mbid = snapshot.ResolvedMetadata.Mbid,
                        DurationMs = snapshot.ResolvedMetadata.DurationMs
                    };
                document.AppleReference = snapshot.AppleReference is null ? null : ToDocument(snapshot.AppleReference);
                document.YouTubeMusicReference = snapshot.YouTubeMusicReference is null ? null : ToDocument(snapshot.YouTubeMusicReference);
                document.IsPlayable = snapshot.IsPlayable;
                document.ProjectionVersion = snapshot.ProjectionVersion;
            });
    }

    private static RavenProviderReferenceRecordDto ToDocument(ProjectedProviderReference reference) =>
        new()
        {
            Provider = reference.Provider.Value,
            Url = reference.Url.ToString(),
            ExternalId = reference.ExternalId,
            SourceProvider = reference.SourceProvider.Value
        };
}
