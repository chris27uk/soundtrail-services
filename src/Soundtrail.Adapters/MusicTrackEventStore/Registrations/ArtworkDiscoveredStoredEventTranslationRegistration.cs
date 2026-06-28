using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.MusicTrackEventStore.Registrations;

public sealed class ArtworkDiscoveredStoredEventTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterStoredEventPair<ArtworkDiscovered, ArtworkDiscoveredEventDataRecordDto>(
            nameof(ArtworkDiscovered),
            domainEvent => new ArtworkDiscoveredEventDataRecordDto(
                domainEvent.EntityKind.ToString(),
                domainEvent.EntityId,
                domainEvent.Url.ToString(),
                domainEvent.Source,
                domainEvent.ObservedAt),
            dto => new ArtworkDiscovered(
                Enum.Parse<CatalogEntityKind>(dto.EntityKind, ignoreCase: true),
                dto.EntityId,
                new Uri(dto.Url),
                dto.Source,
                dto.ObservedAt),
            domainEvent => domainEvent.ObservedAt);
    }
}
