using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.MusicTrackEventStore.Registrations;

public sealed class ProviderReferenceLookupFailedStoredEventTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterStoredEventPair<ProviderReferenceLookupFailed, ProviderReferenceLookupFailedEventDataRecordDto>(
            nameof(ProviderReferenceLookupFailed),
            domainEvent => new ProviderReferenceLookupFailedEventDataRecordDto(
                domainEvent.Provider.Value,
                domainEvent.SourceProvider.Value,
                domainEvent.ObservedAt),
            dto => new ProviderReferenceLookupFailed(
                ProviderName.From(dto.Provider),
                LookupSource.From(dto.SourceProvider),
                dto.ObservedAt),
            domainEvent => domainEvent.ObservedAt);
    }
}
