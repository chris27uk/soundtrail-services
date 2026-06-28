using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.MusicTrackEventStore.Registrations;

public sealed class TrackDiscoveredStoredEventTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterStoredEventPair<TrackDiscovered, TrackDiscoveredEventDataRecordDto>(
            nameof(TrackDiscovered),
            domainEvent => new TrackDiscoveredEventDataRecordDto(
                domainEvent.Title,
                domainEvent.Artist,
                domainEvent.DurationMs,
                domainEvent.Isrc,
                domainEvent.Mbid,
                domainEvent.SourceProvider.Value,
                domainEvent.ObservedAt),
            dto => new TrackDiscovered(
                dto.Title,
                dto.Artist,
                dto.DurationMs,
                dto.Isrc,
                dto.Mbid,
                LookupSource.From(dto.SourceProvider),
                dto.ObservedAt),
            domainEvent => domainEvent.ObservedAt);
    }
}
