using Raven.Client.Documents;
using Raven.Client.Exceptions;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Translators.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Adapters;

public sealed class RavenKnownTrackRequestedCatalogSearchDiscoveryRepository(IDocumentStore documentStore) : ICatalogSearchDiscoveryRepository
{
    public Task<CatalogSearchDiscoveryEventStream> LoadAsync(
        Domain.Search.MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException("Known track follow-up only operates on known catalog item streams.");

    public async Task<CatalogSearchDiscoveryEventStream> LoadAsync(
        KnownCatalogItem knownItem,
        CancellationToken cancellationToken)
    {
        var persistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(knownItem);
        using var session = documentStore.OpenAsyncSession();
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
            storedEvents.Select(item => item.ToEvent()).ToArray());
    }

    public Task<bool> AppendAsync(
        Domain.Search.MusicSearchCriteria searchCriteria,
        int expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException("Known track follow-up only operates on known catalog item streams.");

    public async Task<bool> AppendAsync(
        KnownCatalogItem knownItem,
        int expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken)
    {
        if (events.Count == 0)
        {
            return true;
        }

        using var session = documentStore.OpenAsyncSession();
        session.Advanced.UseOptimisticConcurrency = true;
        var persistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(knownItem);
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
        metadata.UpdatedAtUtc = events.Max(DiscoveryQueryStoredEventTranslator.GetOccurredAtUtc);

        await session.StoreAsync(metadata, cancellationToken);

        foreach (var storedEvent in DiscoveryQueryStoredEventTranslator.ToStoredEvents(knownItem, events, startingVersion))
        {
            await session.StoreAsync(storedEvent, cancellationToken);
        }

        try
        {
            await session.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return false;
        }

        return true;
    }
}
