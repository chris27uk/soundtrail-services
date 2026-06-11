using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters.Documents;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;

internal static class RavenMappings
{
    public static MusicTrackStream ToDomain(this RavenMusicTrackStreamDocument document) =>
        new(
            document.Version,
            document.Events.Select(ToDomain).ToArray());

    public static RavenMusicTrackEventDocument ToDocument(this IMusicTrackEvent @event) =>
        @event switch
        {
            MinimalTrackInfoDiscovered minimalTrackInfoDiscovered => new RavenMusicTrackEventDocument
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
            ProviderPlaybackReferenceResolved providerPlaybackReferenceResolved => new RavenMusicTrackEventDocument
            {
                Type = nameof(ProviderPlaybackReferenceResolved),
                SourceProvider = providerPlaybackReferenceResolved.SourceProvider.ToString(),
                ObservedAt = providerPlaybackReferenceResolved.ObservedAt,
                Provider = providerPlaybackReferenceResolved.Provider.ToString(),
                ExternalId = providerPlaybackReferenceResolved.ExternalId,
                Url = providerPlaybackReferenceResolved.Url.ToString()
            },
            PlaybackReferencesResolutionRequired playbackReferencesResolutionRequired => new RavenMusicTrackEventDocument
            {
                Type = nameof(PlaybackReferencesResolutionRequired),
                SourceProvider = playbackReferencesResolutionRequired.SourceProvider.ToString(),
                ObservedAt = playbackReferencesResolutionRequired.ObservedAt,
                Priority = playbackReferencesResolutionRequired.Priority.ToString(),
                CorrelationId = playbackReferencesResolutionRequired.CorrelationId,
                MusicCatalogId = playbackReferencesResolutionRequired.MusicCatalogId,
                Isrc = playbackReferencesResolutionRequired.SearchTerm.Isrc,
                Title = playbackReferencesResolutionRequired.SearchTerm.Title,
                Artist = playbackReferencesResolutionRequired.SearchTerm.Artist
            },
            TrackLinkedToAlbum trackLinkedToAlbum => new RavenMusicTrackEventDocument
            {
                Type = nameof(TrackLinkedToAlbum),
                SourceProvider = trackLinkedToAlbum.SourceProvider.ToString(),
                ObservedAt = trackLinkedToAlbum.ObservedAt,
                AlbumId = trackLinkedToAlbum.AlbumId,
                AlbumTitle = trackLinkedToAlbum.AlbumTitle
            },
            TrackLinkedToArtist trackLinkedToArtist => new RavenMusicTrackEventDocument
            {
                Type = nameof(TrackLinkedToArtist),
                SourceProvider = trackLinkedToArtist.SourceProvider.ToString(),
                ObservedAt = trackLinkedToArtist.ObservedAt,
                ArtistId = trackLinkedToArtist.ArtistId,
                ArtistName = trackLinkedToArtist.ArtistName
            },
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, "Unknown music track event.")
        };

    private static IMusicTrackEvent ToDomain(RavenMusicTrackEventDocument dto) =>
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
            nameof(PlaybackReferencesResolutionRequired) => new PlaybackReferencesResolutionRequired(
                MusicCatalogId.From(dto.MusicCatalogId ?? string.Empty),
                Enum.Parse<LookupPriorityBand>(dto.Priority ?? string.Empty, ignoreCase: true),
                CorrelationId.From(dto.CorrelationId ?? string.Empty),
                ProviderName.From(dto.SourceProvider),
                dto.ObservedAt,
                dto.Isrc == null ? MusicSearchTerm.ByTrackArtistAlbum(dto.Title!, dto.Artist!, dto.AlbumTitle) : MusicSearchTerm.ByIsrc(dto.Isrc)),
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
            _ => throw new ArgumentOutOfRangeException(nameof(dto.Type), dto.Type, "Unknown music track event type.")
        };
}
