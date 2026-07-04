using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Events;

namespace Soundtrail.Domain.Catalog;

public sealed class ArtistCatalog
{
    private readonly EventHandlers eventHandlers;
    private readonly List<IDomainEvent> uncommittedEvents = [];
    private readonly Dictionary<string, Album> albums = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Track> tracks = new(StringComparer.Ordinal);
    private ArtistId? artistId;
    private ArtistName? artistName;
    private Uri? artworkUrl;

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

    public void CatalogItemDiscovered(CatalogItem catalogItem)
    {
        catalogItem.Match(
            artist => AddArtist(artist.Artist),
            album => AddAlbum(album.Album),
            track => AddTrack(track.Track));
    }

    private void AddTrack(Track track) => this.Apply(new TrackDiscovered(track, ObservedAt: DateTimeOffset.UtcNow), true);
    
    private void AddAlbum(Album album) => this.Apply(new AlbumDiscovered(album, ObservedAt: DateTimeOffset.UtcNow), true);
    
    private void AddArtist(Artist artist) => this.Apply(new ArtistDiscovered(artist, ObservedAt: DateTimeOffset.UtcNow), true);

    public async Task SaveAsync(
        IEventStreamRepository<ArtistId, IDomainEvent> repository,
        LoadedEventStream<ArtistId, IDomainEvent> stream,
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        var append = await repository.AppendAsync(
            stream,
            uncommittedEvents.AsReadOnly(),
            OperationId.From(commandId.Value),
            cancellationToken);

        if (append.Outcome == AppendOutcome.VersionMismatch)
        {
            throw new InvalidOperationException($"Artist catalog stream concurrency conflict for '{artistId?.Value}'.");
        }

        if (append.Appended || append.Outcome == AppendOutcome.DuplicateOperation)
        {
            uncommittedEvents.Clear();
        }
    }

    private void Apply(IDomainEvent @event, bool isNew)
    {
        eventHandlers.Handle(@event);

        if (isNew)
        {
            uncommittedEvents.Add(@event);
        }
    }

    private EventHandlers CreateHandlers()
    {
        var handlers = new EventHandlers();
        handlers.Register<ArtistDiscovered>(On);
        handlers.Register<AlbumDiscovered>(On);
        handlers.Register<TrackDiscovered>(On);
        handlers.Register<StreamingLocationDiscovered>(On);
        handlers.Register<ArtworkDiscovered>(On);
        return handlers;
    }

    private void On(ArtistDiscovered @event)
    {
        artistId ??= @event.Artist.Id;
        artistName = @event.Artist.Name;
    }

    private void On(AlbumDiscovered @event)
    {
        albums[@event.Album.AlbumId] = @event.Album;
    }

    private void On(TrackDiscovered @event)
    {
        var musicCatalogId = @event.Track.MusicCatalogId;
        var track = GetOrCreateTrack(musicCatalogId);
        track.Title = @event.Track.Title;
        track.ArtistName = @event.Track.ArtistName;
        track.DurationMs = @event.Track.DurationMs;
        track.Isrc = @event.Track.Isrc;
        track.Mbid = @event.Track.Mbid;
        track.UpdatedAt = @event.ObservedAt;
    }

    private void On(StreamingLocationDiscovered @event)
    {
        var musicCatalogId = @event.MusicCatalogId ?? throw new InvalidOperationException("Provider reference facts in artist catalog must include a music catalog id.");
        var track = GetOrCreateTrack(musicCatalogId);
        track.ProviderReferences[@event.Provider.Value] = new StreamingLocation(
            @event.Provider,
            @event.ExternalId,
            @event.Url,
            @event.SourceProvider,
            @event.ObservedAt);
        track.FailedProviders.Remove(@event.Provider.Value);
        track.UpdatedAt = @event.ObservedAt;
    }

    private void On(ArtworkDiscovered @event)
    {
        @event.CatalogItemId.Match(
            trackId => UpdateTrackArtwork(trackId.Value, @event.Url, @event.ObservedAt), 
            _ => UpdateArtistArtwork(@event.Url), 
            albumId => UpdateAlbumArtwork(albumId.Value, @event.Url, @event.ObservedAt));
    }

    private void UpdateAlbumArtwork(AlbumId albumId, Uri eventUrl, DateTimeOffset observedAt)
    {
        if (albums.TryGetValue(albumId.StableValue, out var album))
        {
            album.ArtworkUrl = eventUrl.ToString();
            album.UpdatedAt = observedAt;
        }
    }

    private void UpdateArtistArtwork(Uri artworkUri)
    {
        artworkUrl = artworkUri;
    }

    private void UpdateTrackArtwork(TrackId trackId, Uri eventUrl, DateTimeOffset observedAt)
    {
        if (tracks.TryGetValue(trackId.Value, out var track))
        {
            track.ArtworkUrl = eventUrl.ToString();
            track.UpdatedAt = observedAt;
        }
    }

    private Track GetOrCreateTrack(MusicCatalogId musicCatalogId)
    {
        if (this.tracks.TryGetValue(musicCatalogId.Value, out var track))
        {
            return track;
        }
        
        track = new Track(musicCatalogId);
        this.tracks[musicCatalogId.Value] = track;
        return track;
    }
}
