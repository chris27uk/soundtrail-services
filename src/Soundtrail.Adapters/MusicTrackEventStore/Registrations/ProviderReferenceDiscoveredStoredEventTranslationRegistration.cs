using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.MusicTrackEventStore.Registrations;

public sealed class ProviderReferenceDiscoveredStoredEventTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterStoredEventPair<ProviderReferenceDiscovered, ProviderReferenceDiscoveredEventDataRecordDto>(
            nameof(ProviderReferenceDiscovered),
            domainEvent => new ProviderReferenceDiscoveredEventDataRecordDto(
                domainEvent.Provider.Value,
                domainEvent.ExternalId,
                domainEvent.Url.ToString(),
                domainEvent.SourceProvider.Value,
                domainEvent.ObservedAt),
            dto => new ProviderReferenceDiscovered(
                ProviderName.From(dto.Provider),
                dto.ExternalId,
                new Uri(dto.Url),
                LookupSource.From(dto.SourceProvider),
                dto.ObservedAt),
            domainEvent => domainEvent.ObservedAt);
    }
}
