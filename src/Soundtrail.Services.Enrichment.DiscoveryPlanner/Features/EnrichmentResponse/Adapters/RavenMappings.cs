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
            TrackDiscovered minimalTrackInfoDiscovered => minimalTrackInfoDiscovered.ObservedAt,
            ProviderReferenceDiscovered providerPlaybackReferenceResolved => providerPlaybackReferenceResolved.ObservedAt,
            PlaybackReferencesResolutionRequired playbackReferencesResolutionRequired => playbackReferencesResolutionRequired.ObservedAt,
            AlbumDiscovered trackLinkedToAlbum => trackLinkedToAlbum.ObservedAt,
            ArtistDiscovered trackLinkedToArtist => trackLinkedToArtist.ObservedAt,
            ProviderReferenceLookupFailed providerReferenceLookupFailed => providerReferenceLookupFailed.ObservedAt,
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
                Data = JsonSerializer.Serialize(new TrackDiscoveredEventDataRecordDto(
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
            ProviderReferenceDiscovered providerPlaybackReferenceResolved => new MusicTrackStoredEventRecordDto
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(ProviderReferenceDiscovered),
                Data = JsonSerializer.Serialize(new ProviderReferenceDiscoveredEventDataRecordDto(
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
            AlbumDiscovered trackLinkedToAlbum => new MusicTrackStoredEventRecordDto
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(AlbumDiscovered),
                Data = JsonSerializer.Serialize(new AlbumDiscoveredEventDataRecordDto(
                    trackLinkedToAlbum.AlbumId,
                    trackLinkedToAlbum.AlbumTitle,
                    trackLinkedToAlbum.SourceProvider.Value,
                    trackLinkedToAlbum.ObservedAt)),
                OccurredAtUtc = trackLinkedToAlbum.ObservedAt,
                CausationId = commandId.Value
            },
            ArtistDiscovered trackLinkedToArtist => new MusicTrackStoredEventRecordDto
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(ArtistDiscovered),
                Data = JsonSerializer.Serialize(new ArtistDiscoveredEventDataRecordDto(
                    trackLinkedToArtist.ArtistId,
                    trackLinkedToArtist.ArtistName,
                    trackLinkedToArtist.SourceProvider.Value,
                    trackLinkedToArtist.ObservedAt)),
                OccurredAtUtc = trackLinkedToArtist.ObservedAt,
                CausationId = commandId.Value
            },
            ProviderReferenceLookupFailed providerReferenceLookupFailed => new MusicTrackStoredEventRecordDto
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, version),
                MusicCatalogId = musicCatalogId.Value,
                Version = version,
                EventType = nameof(ProviderReferenceLookupFailed),
                Data = JsonSerializer.Serialize(new ProviderReferenceLookupFailedEventDataRecordDto(
                    providerReferenceLookupFailed.Provider.Value,
                    providerReferenceLookupFailed.SourceProvider.Value,
                    providerReferenceLookupFailed.ObservedAt)),
                OccurredAtUtc = providerReferenceLookupFailed.ObservedAt,
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
            _ => throw new ArgumentOutOfRangeException(nameof(dto.EventType), dto.EventType, "Unknown music track event type.")
        };

    private static TrackDiscovered TrackDiscovered(MusicTrackStoredEventRecordDto dto)
    {
        var data = JsonSerializer.Deserialize<TrackDiscoveredEventDataRecordDto>(dto.Data)
            ?? throw new InvalidOperationException("Unable to deserialize minimal track info event data.");
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
        var data = JsonSerializer.Deserialize<ProviderReferenceDiscoveredEventDataRecordDto>(dto.Data)
            ?? throw new InvalidOperationException("Unable to deserialize provider playback reference event data.");
        return new ProviderReferenceDiscovered(
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

    private static AlbumDiscovered AlbumDiscovered(MusicTrackStoredEventRecordDto dto)
    {
        var data = JsonSerializer.Deserialize<AlbumDiscoveredEventDataRecordDto>(dto.Data)
            ?? throw new InvalidOperationException("Unable to deserialize track linked to album event data.");
        return new AlbumDiscovered(
            data.AlbumId,
            data.AlbumTitle,
            ProviderName.From(data.SourceProvider),
            data.ObservedAt);
    }

    private static ArtistDiscovered ArtistDiscovered(MusicTrackStoredEventRecordDto dto)
    {
        var data = JsonSerializer.Deserialize<ArtistDiscoveredEventDataRecordDto>(dto.Data)
            ?? throw new InvalidOperationException("Unable to deserialize track linked to artist event data.");
        return new ArtistDiscovered(
            data.ArtistId,
            data.ArtistName,
            ProviderName.From(data.SourceProvider),
            data.ObservedAt);
    }

    private static ProviderReferenceLookupFailed ProviderReferenceLookupFailed(MusicTrackStoredEventRecordDto dto)
    {
        var data = JsonSerializer.Deserialize<ProviderReferenceLookupFailedEventDataRecordDto>(dto.Data)
            ?? throw new InvalidOperationException("Unable to deserialize provider reference lookup failed event data.");
        return new ProviderReferenceLookupFailed(
            ProviderName.From(data.Provider),
            ProviderName.From(data.SourceProvider),
            data.ObservedAt);
    }
}
