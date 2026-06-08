using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven.Documents;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven;

internal static class RavenMappings
{
    public static MusicTrackStream ToDomain(this RavenMusicTrackStreamDocument document) =>
        new(
            document.Version,
            document.Facts.Select(ToDomain).ToArray());

    public static RavenMusicTrackFactDocument ToDocument(this MusicTrackFact fact) =>
        fact switch
        {
            MinimalTrackInfoDiscovered minimalTrackInfoDiscovered => new RavenMusicTrackFactDocument
            {
                Type = nameof(MinimalTrackInfoDiscovered),
                SourceProvider = minimalTrackInfoDiscovered.SourceProvider.ToString(),
                ObservedAt = minimalTrackInfoDiscovered.ObservedAt,
                Title = minimalTrackInfoDiscovered.Title,
                Artist = minimalTrackInfoDiscovered.Artist,
                DurationMs = minimalTrackInfoDiscovered.DurationMs,
                Isrc = minimalTrackInfoDiscovered.Isrc,
                Mbid = minimalTrackInfoDiscovered.Mbid
            },
            ProviderPlaybackReferenceResolved providerPlaybackReferenceResolved => new RavenMusicTrackFactDocument
            {
                Type = nameof(ProviderPlaybackReferenceResolved),
                SourceProvider = providerPlaybackReferenceResolved.SourceProvider.ToString(),
                ObservedAt = providerPlaybackReferenceResolved.ObservedAt,
                Provider = providerPlaybackReferenceResolved.Provider.ToString(),
                ExternalId = providerPlaybackReferenceResolved.ExternalId,
                Url = providerPlaybackReferenceResolved.Url.ToString()
            },
            AppleMusicResolutionRequired appleMusicResolutionRequired => new RavenMusicTrackFactDocument
            {
                Type = nameof(AppleMusicResolutionRequired),
                SourceProvider = appleMusicResolutionRequired.SourceProvider.ToString(),
                ObservedAt = appleMusicResolutionRequired.ObservedAt,
                Priority = appleMusicResolutionRequired.Priority.ToString(),
                CorrelationId = appleMusicResolutionRequired.CorrelationId,
                MusicCatalogId = appleMusicResolutionRequired.MusicCatalogId
            },
            YouTubeMusicResolutionRequired youTubeMusicResolutionRequired => new RavenMusicTrackFactDocument
            {
                Type = nameof(YouTubeMusicResolutionRequired),
                SourceProvider = youTubeMusicResolutionRequired.SourceProvider.ToString(),
                ObservedAt = youTubeMusicResolutionRequired.ObservedAt,
                Priority = youTubeMusicResolutionRequired.Priority.ToString(),
                CorrelationId = youTubeMusicResolutionRequired.CorrelationId,
                MusicCatalogId = youTubeMusicResolutionRequired.MusicCatalogId
            },
            TrackLinkedToAlbum trackLinkedToAlbum => new RavenMusicTrackFactDocument
            {
                Type = nameof(TrackLinkedToAlbum),
                SourceProvider = trackLinkedToAlbum.SourceProvider.ToString(),
                ObservedAt = trackLinkedToAlbum.ObservedAt,
                AlbumId = trackLinkedToAlbum.AlbumId,
                AlbumTitle = trackLinkedToAlbum.AlbumTitle
            },
            TrackLinkedToArtist trackLinkedToArtist => new RavenMusicTrackFactDocument
            {
                Type = nameof(TrackLinkedToArtist),
                SourceProvider = trackLinkedToArtist.SourceProvider.ToString(),
                ObservedAt = trackLinkedToArtist.ObservedAt,
                ArtistId = trackLinkedToArtist.ArtistId,
                ArtistName = trackLinkedToArtist.ArtistName
            },
            _ => throw new ArgumentOutOfRangeException(nameof(fact), fact, "Unknown music track fact.")
        };

    private static MusicTrackFact ToDomain(RavenMusicTrackFactDocument dto) =>
        dto.Type switch
        {
            nameof(MinimalTrackInfoDiscovered) => new MinimalTrackInfoDiscovered(
                dto.Title ?? string.Empty,
                dto.Artist ?? string.Empty,
                dto.DurationMs,
                dto.Isrc,
                dto.Mbid,
                ProviderName.From(dto.SourceProvider),
                dto.ObservedAt),
            nameof(ProviderPlaybackReferenceResolved) => new ProviderPlaybackReferenceResolved(
                ProviderName.From(dto.Provider ?? string.Empty),
                dto.ExternalId,
                new Uri(dto.Url ?? string.Empty),
                ProviderName.From(dto.SourceProvider),
                dto.ObservedAt),
            nameof(AppleMusicResolutionRequired) => new AppleMusicResolutionRequired(
                MusicCatalogId.From(dto.MusicCatalogId ?? string.Empty),
                Enum.Parse<LookupPriorityBand>(dto.Priority ?? string.Empty, ignoreCase: true),
                CorrelationId.From(dto.CorrelationId ?? string.Empty),
                ProviderName.From(dto.SourceProvider),
                dto.ObservedAt),
            nameof(YouTubeMusicResolutionRequired) => new YouTubeMusicResolutionRequired(
                MusicCatalogId.From(dto.MusicCatalogId ?? string.Empty),
                Enum.Parse<LookupPriorityBand>(dto.Priority ?? string.Empty, ignoreCase: true),
                CorrelationId.From(dto.CorrelationId ?? string.Empty),
                ProviderName.From(dto.SourceProvider),
                dto.ObservedAt),
            nameof(TrackLinkedToAlbum) => new TrackLinkedToAlbum(
                dto.AlbumId,
                dto.AlbumTitle,
                ProviderName.From(dto.SourceProvider),
                dto.ObservedAt),
            nameof(TrackLinkedToArtist) => new TrackLinkedToArtist(
                dto.ArtistId,
                dto.ArtistName,
                ProviderName.From(dto.SourceProvider),
                dto.ObservedAt),
            _ => throw new ArgumentOutOfRangeException(nameof(dto.Type), dto.Type, "Unknown music track fact type.")
        };
}
