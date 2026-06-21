using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters.Documents;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectMusicTrackProjection.Adapters;

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
                document.CanonicalMetadata is null
                    ? null
                    : new ProjectedSongMetadata(
                        document.CanonicalMetadata.Title,
                        ArtistName.From(document.CanonicalMetadata.Artist),
                        document.CanonicalMetadata.Isrc,
                        NormalizeCompact(document.CanonicalMetadata.Isrc),
                        document.CanonicalMetadata.Mbid,
                        NormalizeCompact(document.CanonicalMetadata.Mbid),
                        document.CanonicalMetadata.DurationMs),
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
        document.NormalizedArtist = snapshot.Artist.Canonical;
        document.AlbumTitle = snapshot.AlbumTitle.HasValue ? snapshot.AlbumTitle.Value : null;
        document.NormalizedAlbumTitle = snapshot.AlbumTitle.Canonical;
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
        document.CanonicalMetadata = snapshot.CanonicalMetadata is null
            ? null
            : new RavenSongMetadataRecordDto
            {
                Title = snapshot.CanonicalMetadata.Title,
                Artist = snapshot.CanonicalMetadata.Artist.Value,
                Isrc = snapshot.CanonicalMetadata.Isrc,
                Mbid = snapshot.CanonicalMetadata.Mbid,
                DurationMs = snapshot.CanonicalMetadata.DurationMs
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
