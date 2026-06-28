using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.MusicTrackEventStore.Registrations;

public sealed class ArtistDiscoveredStoredEventTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterStoredEventPair<ArtistDiscovered, ArtistDiscoveredEventDataRecordDto>(
            nameof(ArtistDiscovered),
            domainEvent => new ArtistDiscoveredEventDataRecordDto(
                domainEvent.ArtistId,
                domainEvent.ArtistName,
                domainEvent.SourceArtistId,
                domainEvent.SourceProvider.Value,
                domainEvent.ObservedAt),
            dto => new ArtistDiscovered(
                dto.ArtistId,
                dto.ArtistName,
                dto.SourceArtistId,
                LookupSource.From(dto.SourceProvider),
                dto.ObservedAt),
            domainEvent => domainEvent.ObservedAt);
    }
}
