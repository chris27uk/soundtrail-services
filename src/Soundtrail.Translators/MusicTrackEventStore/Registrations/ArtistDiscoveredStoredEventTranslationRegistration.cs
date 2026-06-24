using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;

namespace Soundtrail.Translators.MusicTrackEventStore.Registrations;

public sealed class ArtistDiscoveredStoredEventTranslationRegistration : IMusicTrackStoredEventTranslationRegistration
{
    public void Register(MusicTrackStoredEventTranslationRegistry registry)
    {
        registry.Register<ArtistDiscovered, ArtistDiscoveredEventDataRecordDto>(
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
                ProviderName.From(dto.SourceProvider),
                dto.ObservedAt),
            domainEvent => domainEvent.ObservedAt);
    }
}
