using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Events;

namespace Soundtrail.Translators.MusicTrackEventStore.Registrations;

public sealed class TrackDiscoveredStoredEventTranslationRegistration : IMusicTrackStoredEventTranslationRegistration
{
    public void Register(MusicTrackStoredEventTranslationRegistry registry)
    {
        registry.Register<TrackDiscovered, TrackDiscoveredEventDataRecordDto>(
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
