using Raven.Client.Documents;
using Raven.Client.Exceptions;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;
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
            storedEvents.Select(item => item.ToDomainEvent().Event).ToArray());
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
        metadata.UpdatedAtUtc = events.Max(GetOccurredAtUtc);

        await session.StoreAsync(metadata, cancellationToken);

        foreach (var storedEvent in ToStoredEvents(knownItem, events, startingVersion))
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

    private static IReadOnlyList<DiscoveryQueryStoredEventRecordDto> ToStoredEvents(
        KnownCatalogItem knownItem,
        IReadOnlyCollection<IDomainEvent> events,
        int startingVersion) =>
        events.Select((@event, index) => ToStoredEvent(knownItem, @event, startingVersion + index + 1)).ToArray();

    private static DiscoveryQueryStoredEventRecordDto ToStoredEvent(
        KnownCatalogItem knownItem,
        IDomainEvent @event,
        int version)
    {
        var persistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(knownItem);

        return @event switch
        {
            StreamingLocationsRequired required => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(persistentId, version),
                Criteria = persistentId,
                Version = version,
                EventType = nameof(StreamingLocationsRequired),
                StreamingLocationsRequired = new StreamingLocationsRequiredEventDataRecordDto(
                    required.MusicCatalogId.Value,
                    required.Priority.ToString(),
                    required.CorrelationId.Value,
                    required.SourceProvider.Value,
                    required.ObservedAt,
                    required.SearchCriteria.Kind,
                    required.SearchCriteria.Query,
                    required.SearchCriteria.Isrc,
                    required.SearchCriteria.Title,
                    required.SearchCriteria.Artist,
                    required.SearchCriteria.Album,
                    required.Hierarchy?.ArtistId?.Value,
                    required.Hierarchy?.AlbumId?.Value),
                OccurredAtUtc = required.ObservedAt,
                CorrelationId = required.CorrelationId.Value
            },
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, "Unknown known track follow-up event.")
        };
    }

    private static DateTimeOffset GetOccurredAtUtc(IDomainEvent @event) =>
        @event switch
        {
            StreamingLocationsRequired required => required.ObservedAt,
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, "Unknown known track follow-up event.")
        };
}
