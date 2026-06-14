using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using System.Text.Json;

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
            MinimalTrackInfoDiscovered minimalTrackInfoDiscovered => minimalTrackInfoDiscovered.ObservedAt,
            ProviderPlaybackReferenceResolved providerPlaybackReferenceResolved => providerPlaybackReferenceResolved.ObservedAt,
            PlaybackReferencesResolutionRequired playbackReferencesResolutionRequired => playbackReferencesResolutionRequired.ObservedAt,
            TrackLinkedToAlbum trackLinkedToAlbum => trackLinkedToAlbum.ObservedAt,
            TrackLinkedToArtist trackLinkedToArtist => trackLinkedToArtist.ObservedAt,
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, "Unknown music track event.")
        };

    private static MusicTrackStoredEventRecordDto ToStoredEventRecordDto(
        this IMusicTrackEvent @event,
        MusicCatalogId musicCatalogId,
        int version,
        CommandId commandId) =>
        @event switch
        {
            MinimalTrackInfoDiscovered minimalTrackInfoDiscovered => new MusicTrackStoredEventRecordDto
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(MinimalTrackInfoDiscovered),
                Data = JsonSerializer.Serialize(new MinimalTrackInfoDiscoveredEventDataRecordDto(
                    minimalTrackInfoDiscovered.Title,
                    minimalTrackInfoDiscovered.Artist,
                    minimalTrackInfoDiscovered.DurationMs,
                    minimalTrackInfoDiscovered.Isrc,
                    minimalTrackInfoDiscovered.Mbid,
                    minimalTrackInfoDiscovered.SourceProvider.Value,
                    minimalTrackInfoDiscovered.ObservedAt)),
                OccurredAtUtc = minimalTrackInfoDiscovered.ObservedAt,
                CausationId = commandId.Value
            },
            ProviderPlaybackReferenceResolved providerPlaybackReferenceResolved => new MusicTrackStoredEventRecordDto
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(ProviderPlaybackReferenceResolved),
                Data = JsonSerializer.Serialize(new ProviderPlaybackReferenceResolvedEventDataRecordDto(
                    providerPlaybackReferenceResolved.Provider.Value,
                    providerPlaybackReferenceResolved.ExternalId,
                    providerPlaybackReferenceResolved.Url.ToString(),
                    providerPlaybackReferenceResolved.SourceProvider.Value,
                    providerPlaybackReferenceResolved.ObservedAt)),
                OccurredAtUtc = providerPlaybackReferenceResolved.ObservedAt,
                CausationId = commandId.Value
            },
            PlaybackReferencesResolutionRequired playbackReferencesResolutionRequired => new MusicTrackStoredEventRecordDto
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(PlaybackReferencesResolutionRequired),
                Data = JsonSerializer.Serialize(new PlaybackReferencesResolutionRequiredEventDataRecordDto(
                    playbackReferencesResolutionRequired.MusicCatalogId.Value,
                    playbackReferencesResolutionRequired.Priority.ToString(),
                    playbackReferencesResolutionRequired.CorrelationId.Value,
                    playbackReferencesResolutionRequired.SourceProvider.Value,
                    playbackReferencesResolutionRequired.ObservedAt,
                    playbackReferencesResolutionRequired.SearchTerm.Isrc,
                    playbackReferencesResolutionRequired.SearchTerm.Title,
                    playbackReferencesResolutionRequired.SearchTerm.Artist,
                    playbackReferencesResolutionRequired.SearchTerm.Album)),
                OccurredAtUtc = playbackReferencesResolutionRequired.ObservedAt,
                CorrelationId = playbackReferencesResolutionRequired.CorrelationId.Value,
                CausationId = commandId.Value
            },
            TrackLinkedToAlbum trackLinkedToAlbum => new MusicTrackStoredEventRecordDto
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(TrackLinkedToAlbum),
                Data = JsonSerializer.Serialize(new TrackLinkedToAlbumEventDataRecordDto(
                    trackLinkedToAlbum.AlbumId,
                    trackLinkedToAlbum.AlbumTitle,
                    trackLinkedToAlbum.SourceProvider.Value,
                    trackLinkedToAlbum.ObservedAt)),
                OccurredAtUtc = trackLinkedToAlbum.ObservedAt,
                CausationId = commandId.Value
            },
            TrackLinkedToArtist trackLinkedToArtist => new MusicTrackStoredEventRecordDto
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(TrackLinkedToArtist),
                Data = JsonSerializer.Serialize(new TrackLinkedToArtistEventDataRecordDto(
                    trackLinkedToArtist.ArtistId,
                    trackLinkedToArtist.ArtistName,
                    trackLinkedToArtist.SourceProvider.Value,
                    trackLinkedToArtist.ObservedAt)),
                OccurredAtUtc = trackLinkedToArtist.ObservedAt,
                CausationId = commandId.Value
            },
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, "Unknown music track event.")
        };

    internal static IMusicTrackEvent ToDomainEvent(this MusicTrackStoredEventRecordDto dto) =>
        dto.EventType switch
        {
            nameof(MinimalTrackInfoDiscovered) => MinimalTrackInfoDiscovered(dto),
            nameof(ProviderPlaybackReferenceResolved) => ProviderPlaybackReferenceResolved(dto),
            nameof(PlaybackReferencesResolutionRequired) => PlaybackReferencesResolutionRequired(dto),
            nameof(TrackLinkedToAlbum) => TrackLinkedToAlbum(dto),
            nameof(TrackLinkedToArtist) => TrackLinkedToArtist(dto),
            _ => throw new ArgumentOutOfRangeException(nameof(dto.EventType), dto.EventType, "Unknown music track event type.")
        };

    private static MinimalTrackInfoDiscovered MinimalTrackInfoDiscovered(MusicTrackStoredEventRecordDto dto)
    {
        var data = JsonSerializer.Deserialize<MinimalTrackInfoDiscoveredEventDataRecordDto>(dto.Data)
            ?? throw new InvalidOperationException("Unable to deserialize minimal track info event data.");
        return new MinimalTrackInfoDiscovered(
            data.Title,
            data.Artist,
            data.DurationMs,
            data.Isrc,
            data.Mbid,
            ProviderName.From(data.SourceProvider),
            data.ObservedAt);
    }

    private static ProviderPlaybackReferenceResolved ProviderPlaybackReferenceResolved(MusicTrackStoredEventRecordDto dto)
    {
        var data = JsonSerializer.Deserialize<ProviderPlaybackReferenceResolvedEventDataRecordDto>(dto.Data)
            ?? throw new InvalidOperationException("Unable to deserialize provider playback reference event data.");
        return new ProviderPlaybackReferenceResolved(
            ProviderName.From(data.Provider),
            data.ExternalId,
            new Uri(data.Url),
            ProviderName.From(data.SourceProvider),
            data.ObservedAt);
    }

    private static PlaybackReferencesResolutionRequired PlaybackReferencesResolutionRequired(MusicTrackStoredEventRecordDto dto)
    {
        var data = JsonSerializer.Deserialize<PlaybackReferencesResolutionRequiredEventDataRecordDto>(dto.Data)
            ?? throw new InvalidOperationException("Unable to deserialize playback references resolution required event data.");
        return new PlaybackReferencesResolutionRequired(
            MusicCatalogId.From(data.MusicCatalogId),
            Enum.Parse<LookupPriorityBand>(data.Priority, ignoreCase: true),
            CorrelationId.From(data.CorrelationId),
            ProviderName.From(data.SourceProvider),
            data.ObservedAt,
            data.Isrc is null
                ? MusicSearchTerm.ByTrackArtistAlbum(data.Title ?? string.Empty, data.Artist ?? string.Empty, data.Album)
                : MusicSearchTerm.ByIsrc(data.Isrc));
    }

    private static TrackLinkedToAlbum TrackLinkedToAlbum(MusicTrackStoredEventRecordDto dto)
    {
        var data = JsonSerializer.Deserialize<TrackLinkedToAlbumEventDataRecordDto>(dto.Data)
            ?? throw new InvalidOperationException("Unable to deserialize track linked to album event data.");
        return new TrackLinkedToAlbum(
            data.AlbumId,
            data.AlbumTitle,
            ProviderName.From(data.SourceProvider),
            data.ObservedAt);
    }

    private static TrackLinkedToArtist TrackLinkedToArtist(MusicTrackStoredEventRecordDto dto)
    {
        var data = JsonSerializer.Deserialize<TrackLinkedToArtistEventDataRecordDto>(dto.Data)
            ?? throw new InvalidOperationException("Unable to deserialize track linked to artist event data.");
        return new TrackLinkedToArtist(
            data.ArtistId,
            data.ArtistName,
            ProviderName.From(data.SourceProvider),
            data.ObservedAt);
    }
}
