using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Events;

namespace Soundtrail.Translators.MusicTrackEventStore.Registrations;

public sealed class ProviderReferenceLookupFailedStoredEventTranslationRegistration : IMusicTrackStoredEventTranslationRegistration
{
    public void Register(MusicTrackStoredEventTranslationRegistry registry)
    {
        registry.Register<ProviderReferenceLookupFailed, ProviderReferenceLookupFailedEventDataRecordDto>(
            nameof(ProviderReferenceLookupFailed),
            domainEvent => new ProviderReferenceLookupFailedEventDataRecordDto(
                domainEvent.Provider.Value,
                domainEvent.SourceProvider.Value,
                domainEvent.ObservedAt),
            dto => new ProviderReferenceLookupFailed(
                ProviderName.From(dto.Provider),
                ProviderName.From(dto.SourceProvider),
                dto.ObservedAt),
            domainEvent => domainEvent.ObservedAt);
    }
}
