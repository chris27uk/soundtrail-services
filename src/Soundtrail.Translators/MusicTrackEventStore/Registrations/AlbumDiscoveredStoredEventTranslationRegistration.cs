using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;

namespace Soundtrail.Translators.MusicTrackEventStore.Registrations;

public sealed class AlbumDiscoveredStoredEventTranslationRegistration : IMusicTrackStoredEventTranslationRegistration
{
    public void Register(MusicTrackStoredEventTranslationRegistry registry)
    {
        registry.Register<AlbumDiscovered, AlbumDiscoveredEventDataRecordDto>(
            nameof(AlbumDiscovered),
            domainEvent => new AlbumDiscoveredEventDataRecordDto(
                domainEvent.AlbumId,
                domainEvent.AlbumTitle,
                domainEvent.SourceAlbumId,
                domainEvent.ReleaseDate,
                domainEvent.SourceProvider.Value,
                domainEvent.ObservedAt),
            dto => new AlbumDiscovered(
                dto.AlbumId,
                dto.AlbumTitle,
                dto.SourceAlbumId,
                dto.ReleaseDate,
                ProviderName.From(dto.SourceProvider),
                dto.ObservedAt),
            domainEvent => domainEvent.ObservedAt);
    }
}
