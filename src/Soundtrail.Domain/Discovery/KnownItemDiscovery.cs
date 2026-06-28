using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery;

public sealed class KnownItemDiscovery
{
    private readonly EventHandlers<KnownItemDiscovery> eventHandlers;
    private readonly List<IDomainEvent> uncommittedEvents = [];
    private KnownCatalogItem? knownItem;
    private bool hasKnownTrackRequested;
    private bool hasArtistCatalogLookupRequested;
    private bool hasAlbumCatalogLookupRequested;

    private KnownItemDiscovery(IEnumerable<IDomainEvent> events)
    {
        eventHandlers = CreateHandlers();

        foreach (var @event in events)
        {
            Apply(@event, isNew: false);
        }
    }

    public static async Task<(LoadedEventStream<DiscoveryQueryKey, IDomainEvent> Stream, KnownItemDiscovery Aggregate)> LoadAsync(
        IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> repository,
        KnownCatalogItem knownItem,
        CancellationToken cancellationToken)
    {
        var stream = await repository.LoadAsync(DiscoveryQueryKey.For(knownItem), cancellationToken);
        var aggregate = new KnownItemDiscovery(stream.Events);
        aggregate.knownItem ??= knownItem;
        return (stream, aggregate);
    }

    public bool TrackRequested(
        TrackId trackId,
        PlaybackProviderFilter playback,
        DateTimeOffset requestedAt,
        CorrelationId correlationId)
    {
        if (hasKnownTrackRequested)
        {
            return false;
        }

        Apply(
            new Events.KnownTrackRequested(
                trackId,
                playback,
                requestedAt,
                correlationId),
            isNew: true);

        return true;
    }

    public bool ArtistRequested(
        ArtistId artistId,
        DateTimeOffset requestedAt,
        CorrelationId correlationId)
    {
        if (hasArtistCatalogLookupRequested)
        {
            return false;
        }

        Apply(
            new ArtistCatalogLookupRequested(
                artistId,
                requestedAt,
                correlationId),
            isNew: true);

        return true;
    }

    public bool AlbumRequested(
        ArtistId? artistId,
        AlbumId albumId,
        DateTimeOffset requestedAt,
        CorrelationId correlationId)
    {
        if (hasAlbumCatalogLookupRequested)
        {
            return false;
        }

        Apply(
            new AlbumCatalogLookupRequested(
                artistId,
                albumId,
                requestedAt,
                correlationId),
            isNew: true);

        return true;
    }

    public async Task<bool> SaveAsync(
        IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> repository,
        LoadedEventStream<DiscoveryQueryKey, IDomainEvent> stream,
        CancellationToken cancellationToken)
    {
        if (uncommittedEvents.Count == 0)
        {
            return true;
        }

        _ = knownItem ?? throw new InvalidOperationException("Known catalog item has not been established.");

        var saved = (await repository.AppendAsync(
            stream,
            uncommittedEvents.AsReadOnly(),
            null,
            cancellationToken)).Appended;

        if (saved)
        {
            uncommittedEvents.Clear();
        }

        return saved;
    }

    private void Apply(IDomainEvent @event, bool isNew)
    {
        eventHandlers.Handle(@event);

        if (isNew)
        {
            uncommittedEvents.Add(@event);
        }
    }

    private EventHandlers<KnownItemDiscovery> CreateHandlers()
    {
        var handlers = new EventHandlers<KnownItemDiscovery>();
        handlers.Register<Events.KnownTrackRequested>(On);
        handlers.Register<ArtistCatalogLookupRequested>(On);
        handlers.Register<AlbumCatalogLookupRequested>(On);
        return handlers;
    }

    private void On(Events.KnownTrackRequested @event)
    {
        knownItem ??= KnownCatalogItem.ForTrack(@event.TrackId);
        hasKnownTrackRequested = true;
    }

    private void On(ArtistCatalogLookupRequested @event)
    {
        knownItem ??= KnownCatalogItem.ForArtist(@event.ArtistId);
        hasArtistCatalogLookupRequested = true;
    }

    private void On(AlbumCatalogLookupRequested @event)
    {
        knownItem ??= KnownCatalogItem.ForAlbum(@event.AlbumId);
        hasAlbumCatalogLookupRequested = true;
    }
}
