using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Support;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters.Mappers;
using Soundtrail.Translators.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;

public sealed class RavenMusicCatalogMetadataFetchedCatalogSearchDiscoveryRepository(
    IAsyncDocumentSession session) : ICatalogSearchDiscoveryRepository,
    ICompleteTrackedDiscoveriesRepository
{
    public Task<CatalogSearchDiscoveryEventStream> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken) =>
        LoadAsync(MusicSeekOrSearchCriteria.FromSearch(searchCriteria), cancellationToken);

    public async Task<CatalogSearchDiscoveryEventStream> LoadAsync(
        MusicSeekOrSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var persistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(criteria);
        var metadata = await session.LoadAsync<DiscoveryQueryEventStreamMetadataRecordDto>(
            DiscoveryQueryEventStreamMetadataRecordDto.GetDocumentId(persistentId),
            cancellationToken);

        if (metadata is null)
        {
            return new CatalogSearchDiscoveryEventStream(0, []);
        }

        var storedEvents = (await session.Advanced.LoadStartingWithAsync<DiscoveryQueryStoredEventRecordDto>(
                $"discovery-query-events/{persistentId}/"))
            .OrderBy(x => x.Version)
            .ToList();

        return new CatalogSearchDiscoveryEventStream(
            metadata.Version,
            storedEvents.Select(CatalogSearchDiscoveryEventRecordMapper.ToDomainEvent).ToArray());
    }

    public Task<bool> AppendAsync(
        MusicSearchCriteria searchCriteria,
        int expectedVersion,
        IReadOnlyCollection<Soundtrail.Domain.Events.IDomainEvent> events,
        CancellationToken cancellationToken) =>
        AppendAsync(MusicSeekOrSearchCriteria.FromSearch(searchCriteria), expectedVersion, events, cancellationToken);

    public async Task<bool> AppendAsync(
        MusicSeekOrSearchCriteria criteria,
        int expectedVersion,
        IReadOnlyCollection<Soundtrail.Domain.Events.IDomainEvent> events,
        CancellationToken cancellationToken)
    {
        if (events.Count == 0)
        {
            return true;
        }

        session.Advanced.UseOptimisticConcurrency = true;
        var persistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(criteria);

        var metadataId = DiscoveryQueryEventStreamMetadataRecordDto.GetDocumentId(persistentId);
        var metadata = await session.LoadAsync<DiscoveryQueryEventStreamMetadataRecordDto>(metadataId, cancellationToken)
            ?? new DiscoveryQueryEventStreamMetadataRecordDto
            {
                Id = metadataId,
                Criteria = persistentId
            };

        if (metadata.Version != expectedVersion)
        {
            return false;
        }

        var startingVersion = metadata.Version;
        metadata.Version += events.Count;
        metadata.UpdatedAtUtc = events.Max(CatalogSearchDiscoveryEventRecordMapper.GetOccurredAtUtc);

        await session.StoreAsync(metadata, cancellationToken);

        foreach (var storedEvent in CatalogSearchDiscoveryEventRecordMapper.ToStoredEvents(criteria, events, startingVersion))
        {
            await session.StoreAsync(storedEvent, cancellationToken);
        }

        return true;
    }
}
