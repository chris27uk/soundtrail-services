using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;

internal static class RavenMappings
{
    public static MusicTrackStream ToDomain(this IReadOnlyList<MusicTrackStoredEventRecordDto> events, int version) =>
        new(
            version,
            events.Select(ToDomainEvent).ToArray());

    public static IReadOnlyList<MusicTrackStoredEventRecordDto> ToStoredEventRecordDtos(
        this IReadOnlyList<IMusicTrackEvent> events,
        MusicCatalogId musicCatalogId,
        int startingVersion,
        CommandId commandId) =>
        events.Select((@event, index) => @event.ToStoredEventRecordDto(musicCatalogId, startingVersion + index + 1, commandId))
            .ToArray();

    public static DateTimeOffset OccurredAtUtc(this IMusicTrackEvent @event) =>
        @event switch
        {
            TrackDiscovered minimalTrackInfoDiscovered => minimalTrackInfoDiscovered.ObservedAt,
            ProviderReferenceDiscovered providerPlaybackReferenceResolved => providerPlaybackReferenceResolved.ObservedAt,
            PlaybackReferencesResolutionRequired playbackReferencesResolutionRequired => playbackReferencesResolutionRequired.ObservedAt,
            AlbumDiscovered trackLinkedToAlbum => trackLinkedToAlbum.ObservedAt,
            ArtistDiscovered trackLinkedToArtist => trackLinkedToArtist.ObservedAt,
            ProviderReferenceLookupFailed providerReferenceLookupFailed => providerReferenceLookupFailed.ObservedAt,
            ArtworkDiscovered artworkDiscovered => artworkDiscovered.ObservedAt,
            MetadataCorrected metadataCorrected => metadataCorrected.CorrectedAt,
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, "Unknown music track event.")
        };

    private static MusicTrackStoredEventRecordDto ToStoredEventRecordDto(
        this IMusicTrackEvent @event,
        MusicCatalogId musicCatalogId,
        int version,
        CommandId commandId) =>
        @event switch
        {
            TrackDiscovered minimalTrackInfoDiscovered => new MusicTrackStoredEventRecordDto
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(TrackDiscovered),
                TrackDiscovered = new TrackDiscoveredEventDataRecordDto(
                    minimalTrackInfoDiscovered.Title,
                    minimalTrackInfoDiscovered.Artist,
                    minimalTrackInfoDiscovered.DurationMs,
                    minimalTrackInfoDiscovered.Isrc,
                    minimalTrackInfoDiscovered.Mbid,
                    minimalTrackInfoDiscovered.SourceProvider.Value,
                    minimalTrackInfoDiscovered.ObservedAt),
                OccurredAtUtc = minimalTrackInfoDiscovered.ObservedAt,
                CausationId = commandId.Value
            },
            ProviderReferenceDiscovered providerPlaybackReferenceResolved => new MusicTrackStoredEventRecordDto
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(ProviderReferenceDiscovered),
                ProviderReferenceDiscovered = new ProviderReferenceDiscoveredEventDataRecordDto(
                    providerPlaybackReferenceResolved.Provider.Value,
                    providerPlaybackReferenceResolved.ExternalId,
                    providerPlaybackReferenceResolved.Url.ToString(),
                    providerPlaybackReferenceResolved.SourceProvider.Value,
                    providerPlaybackReferenceResolved.ObservedAt),
                OccurredAtUtc = providerPlaybackReferenceResolved.ObservedAt,
                CausationId = commandId.Value
            },
            PlaybackReferencesResolutionRequired playbackReferencesResolutionRequired => new MusicTrackStoredEventRecordDto
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(PlaybackReferencesResolutionRequired),
                PlaybackReferencesResolutionRequired = new PlaybackReferencesResolutionRequiredEventDataRecordDto(
                    playbackReferencesResolutionRequired.MusicCatalogId.Value,
                    playbackReferencesResolutionRequired.Priority.ToString(),
                    playbackReferencesResolutionRequired.CorrelationId.Value,
                    playbackReferencesResolutionRequired.SourceProvider.Value,
                    playbackReferencesResolutionRequired.ObservedAt,
                    playbackReferencesResolutionRequired.SearchTerm.Isrc,
                    playbackReferencesResolutionRequired.SearchTerm.Title,
                    playbackReferencesResolutionRequired.SearchTerm.Artist,
                    playbackReferencesResolutionRequired.SearchTerm.Album,
                    playbackReferencesResolutionRequired.Hierarchy?.ArtistId?.Value,
                    playbackReferencesResolutionRequired.Hierarchy?.AlbumId?.Value),
                OccurredAtUtc = playbackReferencesResolutionRequired.ObservedAt,
                CorrelationId = playbackReferencesResolutionRequired.CorrelationId.Value,
                CausationId = commandId.Value
            },
            AlbumDiscovered trackLinkedToAlbum => new MusicTrackStoredEventRecordDto
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(AlbumDiscovered),
                AlbumDiscovered = new AlbumDiscoveredEventDataRecordDto(
                    trackLinkedToAlbum.AlbumId,
                    trackLinkedToAlbum.AlbumTitle,
                    trackLinkedToAlbum.SourceAlbumId,
                    trackLinkedToAlbum.ReleaseDate,
                    trackLinkedToAlbum.SourceProvider.Value,
                    trackLinkedToAlbum.ObservedAt),
                OccurredAtUtc = trackLinkedToAlbum.ObservedAt,
                CausationId = commandId.Value
            },
            ArtistDiscovered trackLinkedToArtist => new MusicTrackStoredEventRecordDto
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(ArtistDiscovered),
                ArtistDiscovered = new ArtistDiscoveredEventDataRecordDto(
                    trackLinkedToArtist.ArtistId,
                    trackLinkedToArtist.ArtistName,
                    trackLinkedToArtist.SourceArtistId,
                    trackLinkedToArtist.SourceProvider.Value,
                    trackLinkedToArtist.ObservedAt),
                OccurredAtUtc = trackLinkedToArtist.ObservedAt,
                CausationId = commandId.Value
            },
            ProviderReferenceLookupFailed providerReferenceLookupFailed => new MusicTrackStoredEventRecordDto
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(ProviderReferenceLookupFailed),
                ProviderReferenceLookupFailed = new ProviderReferenceLookupFailedEventDataRecordDto(
                    providerReferenceLookupFailed.Provider.Value,
                    providerReferenceLookupFailed.SourceProvider.Value,
                    providerReferenceLookupFailed.ObservedAt),
                OccurredAtUtc = providerReferenceLookupFailed.ObservedAt,
                CausationId = commandId.Value
            },
            ArtworkDiscovered artworkDiscovered => new MusicTrackStoredEventRecordDto
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(ArtworkDiscovered),
                ArtworkDiscovered = new ArtworkDiscoveredEventDataRecordDto(
                    artworkDiscovered.EntityKind.ToString(),
                    artworkDiscovered.EntityId,
                    artworkDiscovered.Url.ToString(),
                    artworkDiscovered.Source,
                    artworkDiscovered.ObservedAt),
                OccurredAtUtc = artworkDiscovered.ObservedAt,
                CausationId = commandId.Value
            },
            MetadataCorrected metadataCorrected => new MusicTrackStoredEventRecordDto
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(MetadataCorrected),
                MetadataCorrected = new MetadataCorrectedEventDataRecordDto(
                    metadataCorrected.Title,
                    metadataCorrected.ArtistName,
                    metadataCorrected.ArtistId,
                    metadataCorrected.AlbumTitle,
                    metadataCorrected.AlbumId,
                    metadataCorrected.DurationMs,
                    metadataCorrected.Isrc,
                    metadataCorrected.Mbid,
                    metadataCorrected.Source,
                    metadataCorrected.CorrectedAt),
                OccurredAtUtc = metadataCorrected.CorrectedAt,
                CausationId = commandId.Value
            },
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, "Unknown music track event.")
        };

    internal static IMusicTrackEvent ToDomainEvent(this MusicTrackStoredEventRecordDto dto) =>
        dto.EventType switch
        {
            nameof(TrackDiscovered) => TrackDiscovered(dto),
            nameof(ProviderReferenceDiscovered) => ProviderReferenceDiscovered(dto),
            nameof(PlaybackReferencesResolutionRequired) => PlaybackReferencesResolutionRequired(dto),
            nameof(AlbumDiscovered) => AlbumDiscovered(dto),
            nameof(ArtistDiscovered) => ArtistDiscovered(dto),
            nameof(ProviderReferenceLookupFailed) => ProviderReferenceLookupFailed(dto),
            nameof(ArtworkDiscovered) => ArtworkDiscovered(dto),
            nameof(MetadataCorrected) => MetadataCorrected(dto),
            _ => throw new ArgumentOutOfRangeException(nameof(dto.EventType), dto.EventType, "Unknown music track event type.")
        };

    private static TrackDiscovered TrackDiscovered(MusicTrackStoredEventRecordDto dto)
    {
        var data = dto.TrackDiscovered
            ?? throw new InvalidOperationException("Missing track discovered event data.");
        return new TrackDiscovered(
            data.Title,
            data.Artist,
            data.DurationMs,
            data.Isrc,
            data.Mbid,
            ProviderName.From(data.SourceProvider),
            data.ObservedAt);
    }

    private static ProviderReferenceDiscovered ProviderReferenceDiscovered(MusicTrackStoredEventRecordDto dto)
    {
        var data = dto.ProviderReferenceDiscovered
            ?? throw new InvalidOperationException("Missing provider reference discovered event data.");
        return new ProviderReferenceDiscovered(
            ProviderName.From(data.Provider),
            data.ExternalId,
            new Uri(data.Url),
            ProviderName.From(data.SourceProvider),
            data.ObservedAt);
    }

    private static PlaybackReferencesResolutionRequired PlaybackReferencesResolutionRequired(MusicTrackStoredEventRecordDto dto)
    {
        var data = dto.PlaybackReferencesResolutionRequired
            ?? throw new InvalidOperationException("Missing playback references resolution required event data.");
        return new PlaybackReferencesResolutionRequired(
            MusicCatalogId.From(data.MusicCatalogId),
            Enum.Parse<LookupPriorityBand>(data.Priority, ignoreCase: true),
            CorrelationId.From(data.CorrelationId),
            ProviderName.From(data.SourceProvider),
            data.ObservedAt,
            data.Isrc is null
                ? MusicSearchTerm.ByTrackArtistAlbum(data.Title ?? string.Empty, data.Artist ?? string.Empty, data.Album)
                : MusicSearchTerm.ByIsrc(data.Isrc),
            data.ArtistId is null && data.AlbumId is null
                ? null
                : new CatalogTrackHierarchy(
                    data.ArtistId is null ? null : ArtistId.From(data.ArtistId),
                    data.AlbumId is null ? null : AlbumId.From(data.AlbumId)));
    }

    private static AlbumDiscovered AlbumDiscovered(MusicTrackStoredEventRecordDto dto)
    {
        var data = dto.AlbumDiscovered
            ?? throw new InvalidOperationException("Missing album discovered event data.");
        return new AlbumDiscovered(
            data.AlbumId,
            data.AlbumTitle,
            data.SourceAlbumId,
            data.ReleaseDate,
            ProviderName.From(data.SourceProvider),
            data.ObservedAt);
    }

    private static ArtistDiscovered ArtistDiscovered(MusicTrackStoredEventRecordDto dto)
    {
        var data = dto.ArtistDiscovered
            ?? throw new InvalidOperationException("Missing artist discovered event data.");
        return new ArtistDiscovered(
            data.ArtistId,
            data.ArtistName,
            data.SourceArtistId,
            ProviderName.From(data.SourceProvider),
            data.ObservedAt);
    }

    private static ProviderReferenceLookupFailed ProviderReferenceLookupFailed(MusicTrackStoredEventRecordDto dto)
    {
        var data = dto.ProviderReferenceLookupFailed
            ?? throw new InvalidOperationException("Missing provider reference lookup failed event data.");
        return new ProviderReferenceLookupFailed(
            ProviderName.From(data.Provider),
            ProviderName.From(data.SourceProvider),
            data.ObservedAt);
    }

    private static ArtworkDiscovered ArtworkDiscovered(MusicTrackStoredEventRecordDto dto)
    {
        var data = dto.ArtworkDiscovered
            ?? throw new InvalidOperationException("Missing artwork discovered event data.");
        return new ArtworkDiscovered(
            Enum.Parse<Domain.Catalog.CatalogEntityKind>(data.EntityKind, ignoreCase: true),
            data.EntityId,
            new Uri(data.Url),
            data.Source,
            data.ObservedAt);
    }

    private static MetadataCorrected MetadataCorrected(MusicTrackStoredEventRecordDto dto)
    {
        var data = dto.MetadataCorrected
            ?? throw new InvalidOperationException("Missing metadata corrected event data.");
        return new MetadataCorrected(
            data.Title,
            data.ArtistName,
            data.ArtistId,
            data.AlbumTitle,
            data.AlbumId,
            data.DurationMs,
            data.Isrc,
            data.Mbid,
            data.Source,
            data.CorrectedAt);
    }
}
