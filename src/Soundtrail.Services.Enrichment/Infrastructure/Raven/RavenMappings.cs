using Soundtrail.Contracts;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven.Documents;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

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
                CorrelationId = appleMusicResolutionRequired.CorrelationId.Value,
                MusicCatalogId = appleMusicResolutionRequired.MusicCatalogId.Value
            },
            YouTubeMusicResolutionRequired youTubeMusicResolutionRequired => new RavenMusicTrackFactDocument
            {
                Type = nameof(YouTubeMusicResolutionRequired),
                SourceProvider = youTubeMusicResolutionRequired.SourceProvider.ToString(),
                ObservedAt = youTubeMusicResolutionRequired.ObservedAt,
                Priority = youTubeMusicResolutionRequired.Priority.ToString(),
                CorrelationId = youTubeMusicResolutionRequired.CorrelationId.Value,
                MusicCatalogId = youTubeMusicResolutionRequired.MusicCatalogId.Value
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

    private static MusicTrackFact ToDomain(RavenMusicTrackFactDocument fact) =>
        fact.Type switch
        {
            nameof(MinimalTrackInfoDiscovered) => new MinimalTrackInfoDiscovered(
                fact.Title ?? string.Empty,
                fact.Artist ?? string.Empty,
                fact.DurationMs,
                fact.Isrc,
                fact.Mbid,
                ProviderName.From(fact.SourceProvider),
                fact.ObservedAt),
            nameof(ProviderPlaybackReferenceResolved) => new ProviderPlaybackReferenceResolved(
                ProviderName.From(fact.Provider ?? string.Empty),
                fact.ExternalId,
                new Uri(fact.Url ?? string.Empty),
                ProviderName.From(fact.SourceProvider),
                fact.ObservedAt),
            nameof(AppleMusicResolutionRequired) => new AppleMusicResolutionRequired(
                MusicCatalogId.From(fact.MusicCatalogId ?? string.Empty),
                Enum.Parse<LookupPriorityBand>(fact.Priority ?? string.Empty, ignoreCase: true),
                CorrelationId.From(fact.CorrelationId ?? string.Empty),
                ProviderName.From(fact.SourceProvider),
                fact.ObservedAt),
            nameof(YouTubeMusicResolutionRequired) => new YouTubeMusicResolutionRequired(
                MusicCatalogId.From(fact.MusicCatalogId ?? string.Empty),
                Enum.Parse<LookupPriorityBand>(fact.Priority ?? string.Empty, ignoreCase: true),
                CorrelationId.From(fact.CorrelationId ?? string.Empty),
                ProviderName.From(fact.SourceProvider),
                fact.ObservedAt),
            nameof(TrackLinkedToAlbum) => new TrackLinkedToAlbum(
                fact.AlbumId,
                fact.AlbumTitle,
                ProviderName.From(fact.SourceProvider),
                fact.ObservedAt),
            nameof(TrackLinkedToArtist) => new TrackLinkedToArtist(
                fact.ArtistId,
                fact.ArtistName,
                ProviderName.From(fact.SourceProvider),
                fact.ObservedAt),
            _ => throw new ArgumentOutOfRangeException(nameof(fact.Type), fact.Type, "Unknown music track fact type.")
        };
}
