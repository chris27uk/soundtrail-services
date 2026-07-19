using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Catalog.Aggregates;

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
        this.eventHandlers = CreateHandlers();

        foreach (var @event in events)
        {
            Apply(@event, isNew: false);
        }
    }

    public static async Task<(LoadedEventStream<ArtistId> Stream, ArtistCatalog Aggregate)> LoadAsync(
        IEventStreamRepository<ArtistId> repository,
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
            track => AddTrack(track.Track),
            playlist => { });
    }

    public void StreamingLocationDiscovered(TrackId trackId, StreamingLocation streamingLocation)
    {
        this.Apply(
            new Events.StreamingLocationDiscovered(
                new CatalogItemId.Track(trackId),
                new CatalogTrackHierarchy(this.artistId, null),
                streamingLocation.Provider,
                streamingLocation.ExternalId,
                streamingLocation.Url,
                streamingLocation.SourceProvider,
                streamingLocation.ObservedAt),
            true);
    }

    private void AddTrack(Track track) => this.Apply(new TrackDiscovered(track, BuildHierarchy(track), ObservedAt: DateTimeOffset.UtcNow), true);
    
    private void AddAlbum(Album album) => this.Apply(new AlbumDiscovered(album, ObservedAt: DateTimeOffset.UtcNow), true);
    
    private void AddArtist(Artist artist) => this.Apply(new ArtistDiscovered(artist, ObservedAt: DateTimeOffset.UtcNow), true);

    public async Task SaveAsync(
        IEventStreamRepository<ArtistId> repository,
        LoadedEventStream<ArtistId> stream,
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        var append = await repository.AppendAsync(
            stream,
            this.uncommittedEvents.AsReadOnly(),
            OperationId.From(commandId.Value),
            cancellationToken);

        if (append.Outcome == AppendOutcome.VersionMismatch)
        {
            throw new InvalidOperationException($"Artist catalog stream concurrency conflict for '{this.artistId?.Value}'.");
        }

        if (append.Appended || append.Outcome == AppendOutcome.DuplicateOperation)
        {
            this.uncommittedEvents.Clear();
        }
    }

    private void Apply(IDomainEvent @event, bool isNew)
    {
        this.eventHandlers.Handle(@event);

        if (isNew)
        {
            this.uncommittedEvents.Add(@event);
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
        this.artistId ??= @event.Artist.Id;
        this.artistName = @event.Artist.Name;
    }

    private void On(AlbumDiscovered @event)
    {
        this.albums[@event.Album.AlbumId] = @event.Album;
    }

    private void On(TrackDiscovered @event)
    {
        var track = GetOrCreateTrack(@event.Track.TrackId);
        track.Title = @event.Track.Title;
        track.ArtistName = @event.Track.ArtistName;
        track.AlbumId = @event.Hierarchy.AlbumId?.StableValue ?? @event.Track.AlbumId;
        track.AlbumTitle = @event.Track.AlbumTitle;
        track.DurationMs = @event.Track.DurationMs;
        track.Isrc = @event.Track.Isrc;
        track.Mbid = @event.Track.Mbid;
        track.ReleaseDate = @event.Track.ReleaseDate;
        track.ReleaseType = @event.Track.ReleaseType;
        track.UpdatedAt = @event.ObservedAt;
    }

    private void On(StreamingLocationDiscovered @event)
    {
        var trackId = @event.MusicCatalogId?.Match(
            track => track.Id,
            _ => throw new InvalidOperationException("Provider reference facts in artist catalog must refer to a track."),
            _ => throw new InvalidOperationException("Provider reference facts in artist catalog must refer to a track."),
            _ => throw new InvalidOperationException("Provider reference facts in artist catalog must refer to a track."))
            ?? throw new InvalidOperationException("Provider reference facts in artist catalog must include a music catalog id.");
        var track = GetOrCreateTrack(trackId);
        track.ProviderReferences[@event.Provider.Value] = new StreamingLocation(
            @event.Provider,
            @event.ExternalId,
            @event.Url,
            @event.SourceProvider,
            @event.ObservedAt);
        track.FailedProviders.Remove(@event.Provider.Value);
        track.UpdatedAt = @event.ObservedAt;
    }

    private CatalogTrackHierarchy BuildHierarchy(Track track)
    {
        var hierarchyArtistId = this.artistId;
        Soundtrail.Domain.Catalog.Albums.AlbumId? hierarchyAlbumId =
            string.IsNullOrWhiteSpace(track.AlbumId)
                ? null
                : Soundtrail.Domain.Catalog.Albums.AlbumId.From(track.AlbumId);
        return new CatalogTrackHierarchy(hierarchyArtistId, hierarchyAlbumId);
    }

    private void On(ArtworkDiscovered @event)
    {
        @event.CatalogItemId.Match(
            trackId => UpdateTrackArtwork(trackId.Id, @event.Url, @event.ObservedAt), 
            _ => UpdateArtistArtwork(@event.Url), 
            albumId => UpdateAlbumArtwork(albumId.Id, @event.Url, @event.ObservedAt),
            _ => { });
    }

    private void UpdateAlbumArtwork(AlbumId albumId, Uri eventUrl, DateTimeOffset observedAt)
    {
        if (this.albums.TryGetValue(albumId.StableValue, out var album))
        {
            album.ArtworkUrl = eventUrl.ToString();
            album.UpdatedAt = observedAt;
        }
    }

    private void UpdateArtistArtwork(Uri artworkUri)
    {
        this.artworkUrl = artworkUri;
    }

    private void UpdateTrackArtwork(TrackId trackId, Uri eventUrl, DateTimeOffset observedAt)
    {
        if (this.tracks.TryGetValue(trackId.Value, out var track))
        {
            track.ArtworkUrl = eventUrl.ToString();
            track.UpdatedAt = observedAt;
        }
    }

    private Track GetOrCreateTrack(TrackId trackId)
    {
        if (this.tracks.TryGetValue(trackId.Value, out var track))
        {
            return track;
        }
        
        track = new Track(trackId);
        this.tracks[trackId.Value] = track;
        return track;
    }
}
