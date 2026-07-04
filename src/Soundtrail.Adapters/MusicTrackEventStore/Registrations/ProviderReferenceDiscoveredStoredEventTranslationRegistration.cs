using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.MusicTrackEventStore.Registrations;

public sealed class ProviderReferenceDiscoveredStoredEventTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterStoredEventPair<StreamingLocationDiscovered, ProviderReferenceDiscoveredEventDataRecordDto>(
            nameof(StreamingLocationDiscovered),
            domainEvent => new ProviderReferenceDiscoveredEventDataRecordDto(
                domainEvent.MusicCatalogId?.Value,
                domainEvent.Provider.Value,
                domainEvent.ExternalId,
                domainEvent.Url.ToString(),
                domainEvent.SourceProvider.Value,
                domainEvent.ObservedAt),
            dto => new StreamingLocationDiscovered(
                dto.MusicCatalogId is null ? null : MusicCatalogId.From(dto.MusicCatalogId),
                ProviderName.From(dto.Provider),
                dto.ExternalId,
                new Uri(dto.Url),
                LookupSource.From(dto.SourceProvider),
                dto.ObservedAt),
            domainEvent => domainEvent.ObservedAt);
    }
}
