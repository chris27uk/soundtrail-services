using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Domain.Catalog;

public sealed class ArtistCatalog
{
    private readonly EventHandlers<ArtistCatalog> eventHandlers;
    private readonly List<IDomainEvent> uncommittedEvents = [];
    private readonly Dictionary<string, AlbumState> albums = new(StringComparer.Ordinal);
    private ArtistId? artistId;
    private string? artistName;
    private string? sourceArtistId;

    private ArtistCatalog(
        ArtistId artistId,
        IEnumerable<IDomainEvent> events)
    {
        this.artistId = artistId;
        eventHandlers = CreateHandlers();

        foreach (var @event in events)
        {
            Apply(@event, isNew: false);
        }
    }

    public static async Task<(LoadedEventStream<ArtistId, IDomainEvent> Stream, ArtistCatalog Aggregate)> LoadAsync(
        IEventStreamRepository<ArtistId, IDomainEvent> repository,
        ArtistId artistId,
        CancellationToken cancellationToken)
    {
        var stream = await repository.LoadAsync(artistId, cancellationToken);
        return (stream, new ArtistCatalog(artistId, stream.Events));
    }

    public void ArtistMetadataFetched(ArtistMetadataFetched fetched)
    {
        DiscoverArtist(
            fetched.ArtistId,
            fetched.Metadata.ArtistName,
            fetched.Metadata.SourceArtistId,
            fetched.SourceProvider,
            fetched.CreatedAt);
    }

    public void DiscoverArtist(
        ArtistId discoveredArtistId,
        string artistDisplayName,
        string? discoveredSourceArtistId,
        LookupSource sourceProvider,
        DateTimeOffset observedAt)
    {
        if (artistId != discoveredArtistId)
        {
            throw new InvalidOperationException("Artist catalog id does not match fetched artist metadata.");
        }

        if (string.Equals(artistName, artistDisplayName, StringComparison.Ordinal)
            && string.Equals(sourceArtistId, discoveredSourceArtistId, StringComparison.Ordinal))
        {
            return;
        }

        Apply(
            new ArtistDiscovered(
                discoveredArtistId.Value,
                artistDisplayName,
                discoveredSourceArtistId,
                sourceProvider,
                observedAt),
            isNew: true);
    }

    public void AlbumMetadataFetched(AlbumMetadataFetched fetched)
    {
        DiscoverAlbum(
            fetched.ArtistId,
            fetched.AlbumId,
            fetched.Metadata.ArtistName,
            fetched.Metadata.AlbumTitle,
            fetched.Metadata.SourceArtistId,
            fetched.Metadata.SourceAlbumId,
            fetched.Metadata.ReleaseDate,
            fetched.SourceProvider,
            fetched.CreatedAt);
    }

    public void DiscoverAlbum(
        ArtistId discoveredArtistId,
        AlbumId albumId,
        string artistDisplayName,
        string albumTitle,
        string? discoveredSourceArtistId,
        string? discoveredSourceAlbumId,
        DateOnly? releaseDate,
        LookupSource sourceProvider,
        DateTimeOffset observedAt)
    {
        if (artistId != discoveredArtistId)
        {
            throw new InvalidOperationException("Artist catalog id does not match fetched album metadata.");
        }

        DiscoverArtist(
            discoveredArtistId,
            artistDisplayName,
            discoveredSourceArtistId,
            sourceProvider,
            observedAt);

        if (albums.TryGetValue(albumId.Value, out var existing)
            && string.Equals(existing.AlbumTitle, albumTitle, StringComparison.Ordinal)
            && string.Equals(existing.SourceAlbumId, discoveredSourceAlbumId, StringComparison.Ordinal)
            && existing.ReleaseDate == releaseDate)
        {
            return;
        }

        Apply(
            new AlbumDiscovered(
                albumId.Value,
                albumTitle,
                discoveredSourceAlbumId,
                releaseDate,
                sourceProvider,
                observedAt),
            isNew: true);
    }

    public async Task<bool> SaveAsync(
        IEventStreamRepository<ArtistId, IDomainEvent> repository,
        LoadedEventStream<ArtistId, IDomainEvent> stream,
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        if (uncommittedEvents.Count == 0)
        {
            return true;
        }

        var append = await repository.AppendAsync(
            stream,
            uncommittedEvents.AsReadOnly(),
            OperationId.From(commandId.Value),
            cancellationToken);

        if (append.Outcome == AppendOutcome.VersionMismatch)
        {
            throw new InvalidOperationException($"Artist catalog stream concurrency conflict for '{artistId?.Value}'.");
        }

        if (append.Appended)
        {
            uncommittedEvents.Clear();
        }

        return append.Appended || append.Outcome == AppendOutcome.DuplicateOperation;
    }

    private void Apply(IDomainEvent @event, bool isNew)
    {
        eventHandlers.Handle(@event);

        if (isNew)
        {
            uncommittedEvents.Add(@event);
        }
    }

    private EventHandlers<ArtistCatalog> CreateHandlers()
    {
        var handlers = new EventHandlers<ArtistCatalog>();
        handlers.Register<ArtistDiscovered>(@event =>
        {
            artistId ??= ArtistId.From(@event.ArtistId ?? throw new InvalidOperationException("Artist id is required."));
            artistName = @event.ArtistName;
            sourceArtistId = @event.SourceArtistId;
        });
        handlers.Register<AlbumDiscovered>(@event =>
            albums[@event.AlbumId ?? throw new InvalidOperationException("Album id is required.")] =
                new AlbumState(@event.AlbumTitle, @event.SourceAlbumId, @event.ReleaseDate));
        return handlers;
    }

    private sealed record AlbumState(
        string? AlbumTitle,
        string? SourceAlbumId,
        DateOnly? ReleaseDate);
}
