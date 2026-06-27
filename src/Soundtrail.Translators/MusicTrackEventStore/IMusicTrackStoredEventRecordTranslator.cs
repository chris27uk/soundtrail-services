using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Events;

namespace Soundtrail.Translators.MusicTrackEventStore;

public interface IMusicTrackStoredEventRecordTranslator
{
    MusicTrackStoredEventRecordDto ToDto(
        MusicCatalogId musicCatalogId,
        int version,
        CommandId commandId,
        IMusicTrackEvent domainEvent);

    IMusicTrackEvent ToDomainObject(MusicTrackStoredEventRecordDto dto);
}
