using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Projection;

namespace Soundtrail.Adapters.ProjectionDocuments;

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

    private static ProjectedStreamingReference ToDomain(RavenProviderReferenceRecordDto reference) =>
        new(
            ProviderName.From(reference.Provider),
            new Uri(reference.Url),
            reference.ExternalId,
            LookupSource.From(reference.SourceProvider));

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
