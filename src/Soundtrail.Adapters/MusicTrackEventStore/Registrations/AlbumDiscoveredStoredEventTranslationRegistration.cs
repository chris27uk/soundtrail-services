using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.MusicTrackEventStore.Registrations;

public sealed class AlbumDiscoveredStoredEventTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterStoredEventPair<AlbumDiscovered, AlbumDiscoveredEventDataRecordDto>(
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
                LookupSource.From(dto.SourceProvider),
                dto.ObservedAt),
            domainEvent => domainEvent.ObservedAt);
    }
}
