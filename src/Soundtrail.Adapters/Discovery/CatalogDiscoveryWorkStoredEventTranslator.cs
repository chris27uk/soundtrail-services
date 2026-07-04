using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Discovery;

public sealed class CatalogDiscoveryWorkStoredEventTranslator : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterStoredEventPair<CatalogDiscoveryWorkRequested, CatalogDiscoveryWorkRequestedEventDataRecordDto>(
            nameof(CatalogDiscoveryWorkRequested),
            domainEvent => new CatalogDiscoveryWorkRequestedEventDataRecordDto(
                domainEvent.MusicCatalogId.Value,
                domainEvent.TrustLevel,
                domainEvent.RiskScore,
                domainEvent.RequestedAt),
            dto => new CatalogDiscoveryWorkRequested(
                MusicCatalogId.From(dto.MusicCatalogId),
                dto.TrustLevel,
                dto.RiskScore,
                dto.RequestedAtUtc),
            domainEvent => domainEvent.RequestedAt);

        registry.RegisterStoredEventPair<WorkDeferred, CatalogDiscoveryWorkDeferredEventDataRecordDto>(
            nameof(WorkDeferred),
            domainEvent => new CatalogDiscoveryWorkDeferredEventDataRecordDto(
                domainEvent.MusicCatalogId.Value,
                domainEvent.NextEligibleAt,
                domainEvent.Reason,
                domainEvent.DeferredAt),
            dto => new WorkDeferred(
                MusicCatalogId.From(dto.MusicCatalogId),
                dto.NextEligibleAtUtc,
                dto.Reason,
                dto.DeferredAtUtc),
            domainEvent => domainEvent.DeferredAt);

        registry.RegisterStoredEventPair<CatalogDiscoveryWorkIgnored, CatalogDiscoveryWorkIgnoredEventDataRecordDto>(
            nameof(CatalogDiscoveryWorkIgnored),
            domainEvent => new CatalogDiscoveryWorkIgnoredEventDataRecordDto(
                domainEvent.MusicCatalogId.Value,
                domainEvent.NextEligibleAt,
                domainEvent.Reason,
                domainEvent.IgnoredAt),
            dto => new CatalogDiscoveryWorkIgnored(
                MusicCatalogId.From(dto.MusicCatalogId),
                dto.NextEligibleAtUtc,
                dto.Reason,
                dto.IgnoredAtUtc),
            domainEvent => domainEvent.IgnoredAt);

        registry.RegisterStoredEventPair<WorkScheduled, CatalogDiscoveryWorkScheduledEventDataRecordDto>(
            nameof(WorkScheduled),
            domainEvent => new CatalogDiscoveryWorkScheduledEventDataRecordDto(
                domainEvent.MusicCatalogId.Value,
                domainEvent.Priority.ToString(),
                domainEvent.For,
                domainEvent.Reason,
                domainEvent.ScheduledAt),
            dto => new WorkScheduled(
                MusicCatalogId.From(dto.MusicCatalogId),
                Enum.Parse<LookupPriorityBand>(dto.Priority, ignoreCase: true),
                dto.NextEligibleAtUtc,
                dto.Reason,
                dto.ScheduledAtUtc),
            domainEvent => domainEvent.ScheduledAt);
    }
}
