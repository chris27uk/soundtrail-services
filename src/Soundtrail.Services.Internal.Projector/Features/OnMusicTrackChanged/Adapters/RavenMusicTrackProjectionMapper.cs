using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Projection;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.Adapters;

public sealed class RavenMusicTrackProjectionMapper
{
    public MusicTrackProjection ToDomain(RavenTrackRecordDto document)
    {
        return MusicTrackProjection.Load(
            new MusicTrackProjectionSnapshot(
                document.ArtistId,
                document.AlbumId,
                document.Title,
                ArtistName.From(document.Artist),
                AlbumTitle.From(document.AlbumTitle),
                document.SearchText,
                document.Isrc,
                document.NormalizedIsrc,
                document.Mbid,
                document.NormalizedMbid,
                document.AppleId,
                document.SpotifyId,
                document.DurationMs,
                document.ReleaseDate,
                document.ArtworkUrl,
                document.ResolvedMetadata is null
                    ? null
                    : new ProjectedSongMetadata(
                        document.ResolvedMetadata.Title,
                        ArtistName.From(document.ResolvedMetadata.Artist),
                        document.ResolvedMetadata.Isrc,
                        NormalizeCompact(document.ResolvedMetadata.Isrc),
                        document.ResolvedMetadata.Mbid,
                        NormalizeCompact(document.ResolvedMetadata.Mbid),
                        document.ResolvedMetadata.DurationMs),
                document.AppleReference is null ? null : ToDomain(document.AppleReference),
                document.YouTubeMusicReference is null ? null : ToDomain(document.YouTubeMusicReference),
                document.IsPlayable,
                document.ProjectionVersion));
    }

    public RavenTrackRecordDto ToDocument(string documentId, MusicTrackProjection projection)
    {
        var document = new RavenTrackRecordDto
        {
            Id = documentId
        };
        MapOntoDocument(document, projection);
        return document;
    }

    public void MapOntoDocument(RavenTrackRecordDto document, MusicTrackProjection projection)
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
    }

    private static ProjectedProviderReference ToDomain(RavenProviderReferenceRecordDto reference) =>
        new(
            ProviderName.From(reference.Provider),
            new Uri(reference.Url),
            reference.ExternalId,
            ProviderName.From(reference.SourceProvider));

    private static RavenProviderReferenceRecordDto ToDocument(ProjectedProviderReference reference) =>
        new()
        {
            Provider = reference.Provider.Value,
            Url = reference.Url.ToString(),
            ExternalId = reference.ExternalId,
            SourceProvider = reference.SourceProvider.Value
        };

    private static string NormalizeCompact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }
}
