using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Features.JustInTimeScheduling.Adapters.Mappers;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.ProjectDiscoveryLifecycle.Adapters;

public static class DiscoveryQueryStoredEventRecordMapper
{
    public static VersionedCatalogSearchDiscoveryEvent ToDomainEvent(this DiscoveryQueryStoredEventRecordDto dto) =>
        new(dto.Version, CatalogSearchDiscoveryEventRecordMapper.ToDomainEvent(dto));
}
