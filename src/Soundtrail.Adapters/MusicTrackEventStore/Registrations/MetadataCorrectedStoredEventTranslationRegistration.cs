using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Events;

namespace Soundtrail.Adapters.MusicTrackEventStore.Registrations;

public sealed class MetadataCorrectedStoredEventTranslationRegistration : IMusicTrackStoredEventTranslationRegistration
{
    public void Register(MusicTrackStoredEventTranslationRegistry registry)
    {
        registry.Register<MetadataCorrected, MetadataCorrectedEventDataRecordDto>(
            nameof(MetadataCorrected),
            domainEvent => new MetadataCorrectedEventDataRecordDto(
                domainEvent.Title,
                domainEvent.ArtistName,
                domainEvent.ArtistId,
                domainEvent.SourceArtistId,
                domainEvent.AlbumTitle,
                domainEvent.AlbumId,
                domainEvent.SourceAlbumId,
                domainEvent.ReleaseDate,
                domainEvent.DurationMs,
                domainEvent.Isrc,
                domainEvent.Mbid,
                domainEvent.Source,
                domainEvent.CorrectedAt),
            dto => new MetadataCorrected(
                dto.Title,
                dto.ArtistName,
                dto.ArtistId,
                dto.SourceArtistId,
                dto.AlbumTitle,
                dto.AlbumId,
                dto.SourceAlbumId,
                dto.ReleaseDate,
                dto.DurationMs,
                dto.Isrc,
                dto.Mbid,
                dto.Source,
                dto.CorrectedAt),
            domainEvent => domainEvent.CorrectedAt);
    }
}
