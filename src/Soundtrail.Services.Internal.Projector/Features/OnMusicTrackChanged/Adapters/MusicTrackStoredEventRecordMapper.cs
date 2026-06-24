using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.Adapters;

public static class MusicTrackStoredEventRecordMapper
{
    public static IMusicTrackEvent ToDomainEvent(this MusicTrackStoredEventRecordDto dto) =>
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
        var data = dto.StreamingLocationsRequired
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
            data.SourceArtistId,
            data.AlbumTitle,
            data.AlbumId,
            data.SourceAlbumId,
            data.ReleaseDate,
            data.DurationMs,
            data.Isrc,
            data.Mbid,
            data.Source,
            data.CorrectedAt);
    }
}
