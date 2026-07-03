using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Catalog;

public sealed class ArtistCatalog
{
    private readonly EventHandlers<ArtistCatalog> eventHandlers;
    private readonly List<IDomainEvent> uncommittedEvents = [];
    private readonly Dictionary<string, Album> albums = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Track> tracks = new(StringComparer.Ordinal);
    private ArtistId? artistId;
    private string? artistName;
    private string? sourceArtistId;
    private string? artworkUrl;

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

    public void TrackMetadataFetched(ArtistId artist, MusicCatalogMetadataFetched fetched)
    {
        EnsureArtistMatch(artist);

        if (fetched.Metadata is not null && fetched.SourceProvider == LookupSource.MusicBrainz)
        {
            DiscoverArtist(
                artist,
                fetched.Metadata.Artist,
                fetched.Metadata.SourceArtistId,
                fetched.SourceProvider,
                fetched.CreatedAt);

            if (fetched.Hierarchy?.AlbumId is not null)
            {
                DiscoverAlbum(
                    artist,
                    fetched.Hierarchy.AlbumId.Value,
                    fetched.Metadata.Artist,
                    fetched.Metadata.AlbumTitle ?? string.Empty,
                    fetched.Metadata.SourceArtistId,
                    fetched.Metadata.SourceAlbumId,
                    fetched.Metadata.ReleaseDate,
                    fetched.SourceProvider,
                    fetched.CreatedAt);
            }

            DiscoverTrack(
                fetched.MusicCatalogId,
                fetched.Hierarchy?.AlbumId,
                fetched.Metadata.Title,
                fetched.Metadata.Artist,
                fetched.Metadata.AlbumTitle,
                fetched.Metadata.DurationMs,
                fetched.Metadata.Isrc,
                fetched.Metadata.Mbid,
                fetched.Metadata.ReleaseDate,
                fetched.SourceProvider,
                fetched.CreatedAt);
        }

        foreach (var reference in fetched.References)
        {
            DiscoverProviderReference(
                fetched.MusicCatalogId,
                reference.Provider,
                reference.ExternalId,
                reference.Url,
                fetched.SourceProvider,
                fetched.CreatedAt);
        }

        foreach (var failedProvider in fetched.FailedProviders)
        {
            RecordProviderReferenceLookupFailed(
                fetched.MusicCatalogId,
                failedProvider.Provider,
                failedProvider.SourceProvider,
                fetched.CreatedAt);
        }

        if (fetched.Metadata is not null && ShouldRequireStreamingLocations(fetched))
        {
            var searchCriteria = !string.IsNullOrWhiteSpace(fetched.Metadata.Isrc)
                ? MusicSearchCriteria.ByIsrc(fetched.Metadata.Isrc)
                : MusicSearchCriteria.ByTrackArtistAlbum(
                    fetched.Metadata.Title,
                    fetched.Metadata.Artist,
                    album: null);

            Apply(
                new StreamingLocationsRequired(
                    fetched.MusicCatalogId,
                    fetched.Priority,
                    fetched.CorrelationId,
                    fetched.SourceProvider,
                    fetched.CreatedAt,
                    searchCriteria,
                    new CatalogTrackHierarchy(
                        artist,
                        fetched.Hierarchy?.AlbumId)),
                isNew: true);
        }
    }

    public void DiscoverArtist(
        ArtistId discoveredArtistId,
        string artistDisplayName,
        string? discoveredSourceArtistId,
        LookupSource sourceProvider,
        DateTimeOffset observedAt)
    {
        EnsureArtistMatch(discoveredArtistId);

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
        EnsureArtistMatch(discoveredArtistId);

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

    private void DiscoverTrack(
        MusicCatalogId musicCatalogId,
        AlbumId? albumId,
        string title,
        string trackArtistName,
        string? albumTitle,
        int? durationMs,
        string? isrc,
        string? mbid,
        DateOnly? releaseDate,
        LookupSource sourceProvider,
        DateTimeOffset observedAt)
    {
        if (tracks.TryGetValue(musicCatalogId.Value, out var existing)
            && string.Equals(existing.Title, title, StringComparison.Ordinal)
            && string.Equals(existing.ArtistName, trackArtistName, StringComparison.Ordinal)
            && string.Equals(existing.AlbumTitle, albumTitle, StringComparison.Ordinal)
            && string.Equals(existing.AlbumId, albumId?.Value, StringComparison.Ordinal)
            && existing.DurationMs == durationMs
            && string.Equals(existing.Isrc, isrc, StringComparison.Ordinal)
            && string.Equals(existing.Mbid, mbid, StringComparison.Ordinal)
            && existing.ReleaseDate == releaseDate)
        {
            return;
        }

        Apply(
            new TrackDiscovered(
                musicCatalogId,
                title,
                trackArtistName,
                durationMs,
                isrc,
                mbid,
                sourceProvider,
                observedAt),
            isNew: true);

        if (albumId is not null && !string.IsNullOrWhiteSpace(albumTitle))
        {
            Apply(
                new MetadataCorrected(
                    musicCatalogId,
                    title,
                    trackArtistName,
                    RequireArtistId().Value,
                    sourceArtistId,
                    albumTitle,
                    albumId.Value,
                    albums.TryGetValue(albumId.Value, out var albumState) ? albumState.SourceAlbumId : null,
                    releaseDate,
                    durationMs,
                    isrc,
                    mbid,
                    sourceProvider.Value,
                    observedAt),
                isNew: true);
        }
    }

    private void DiscoverProviderReference(
        MusicCatalogId musicCatalogId,
        ProviderName provider,
        string? externalId,
        Uri url,
        LookupSource sourceProvider,
        DateTimeOffset observedAt)
    {
        var track = GetOrCreateTrack(musicCatalogId);
        if (track.ProviderReferences.TryGetValue(provider.Value, out var existing)
            && string.Equals(existing.ExternalId, externalId, StringComparison.Ordinal)
            && existing.Url == url)
        {
            return;
        }

        Apply(
            new ProviderReferenceDiscovered(
                musicCatalogId,
                provider,
                externalId,
                url,
                sourceProvider,
                observedAt),
            isNew: true);
    }

    private void RecordProviderReferenceLookupFailed(
        MusicCatalogId musicCatalogId,
        ProviderName provider,
        LookupSource sourceProvider,
        DateTimeOffset observedAt)
    {
        var track = GetOrCreateTrack(musicCatalogId);
        if (!track.FailedProviders.Add(provider.Value))
        {
            return;
        }

        track.FailedProviders.Remove(provider.Value);

        Apply(
            new ProviderReferenceLookupFailed(
                musicCatalogId,
                provider,
                sourceProvider,
                observedAt),
            isNew: true);
    }

    private bool ShouldRequireStreamingLocations(MusicCatalogMetadataFetched fetched)
    {
        if (fetched.SourceProvider != LookupSource.MusicBrainz || fetched.Metadata is null)
        {
            return false;
        }

        var track = GetOrCreateTrack(fetched.MusicCatalogId);
        return track.ProviderReferences.Count == 0 && !track.StreamingLocationsRequired;
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
        handlers.Register<ArtistDiscovered>(On);
        handlers.Register<AlbumDiscovered>(On);
        handlers.Register<TrackDiscovered>(On);
        handlers.Register<ProviderReferenceDiscovered>(On);
        handlers.Register<ProviderReferenceLookupFailed>(On);
        handlers.Register<StreamingLocationsRequired>(On);
        handlers.Register<ArtworkDiscovered>(On);
        handlers.Register<MetadataCorrected>(On);
        return handlers;
    }

    private void On(ArtistDiscovered @event)
    {
        artistId ??= ArtistId.From(@event.ArtistId ?? throw new InvalidOperationException("Artist id is required."));
        artistName = @event.ArtistName;
        sourceArtistId = @event.SourceArtistId;
    }

    private void On(AlbumDiscovered @event)
    {
        var albumId = @event.AlbumId ?? throw new InvalidOperationException("Album id is required.");
        albums[albumId] = new Album(
            AlbumId.From(albumId),
            @event.AlbumTitle,
            @event.SourceAlbumId,
            @event.ReleaseDate,
            albums.TryGetValue(albumId, out var existing) ? existing.ArtworkUrl : null,
            @event.ObservedAt);
    }

    private void On(TrackDiscovered @event)
    {
        var musicCatalogId = @event.MusicCatalogId
                             ?? throw new InvalidOperationException("Track facts in artist catalog must include a music catalog id.");
        var track = GetOrCreateTrack(musicCatalogId);
        track.Title = @event.Title;
        track.ArtistName = @event.Artist;
        track.DurationMs = @event.DurationMs;
        track.Isrc = @event.Isrc;
        track.Mbid = @event.Mbid;
        track.UpdatedAt = @event.ObservedAt;
    }

    private void On(ProviderReferenceDiscovered @event)
    {
        var musicCatalogId = @event.MusicCatalogId
                             ?? throw new InvalidOperationException("Provider reference facts in artist catalog must include a music catalog id.");
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

    private void On(ProviderReferenceLookupFailed @event)
    {
        var musicCatalogId = @event.MusicCatalogId
                             ?? throw new InvalidOperationException("Provider reference failure facts in artist catalog must include a music catalog id.");
        var track = GetOrCreateTrack(musicCatalogId);
        track.ProviderReferences.Remove(@event.Provider.Value);
        track.FailedProviders.Add(@event.Provider.Value);
        track.UpdatedAt = @event.ObservedAt;
    }

    private void On(StreamingLocationsRequired @event)
    {
        var track = GetOrCreateTrack(@event.MusicCatalogId);
        track.StreamingLocationsRequired = true;
        track.AlbumId ??= @event.Hierarchy?.AlbumId?.Value;
        track.UpdatedAt = @event.ObservedAt;
    }

    private void On(ArtworkDiscovered @event)
    {
        switch (@event.EntityKind)
        {
            case CatalogEntityKind.Artist:
                artworkUrl = @event.Url.ToString();
                break;
            case CatalogEntityKind.Album when !string.IsNullOrWhiteSpace(@event.EntityId):
                if (albums.TryGetValue(@event.EntityId, out var album))
                {
                    album.ArtworkUrl = @event.Url.ToString();
                    album.UpdatedAt = @event.ObservedAt;
                }
                break;
            case CatalogEntityKind.Track when !string.IsNullOrWhiteSpace(@event.EntityId):
                if (tracks.TryGetValue(@event.EntityId, out var track))
                {
                    track.ArtworkUrl = @event.Url.ToString();
                    track.UpdatedAt = @event.ObservedAt;
                }
                break;
        }
    }

    private void On(MetadataCorrected @event)
    {
        var musicCatalogId = @event.MusicCatalogId
                             ?? throw new InvalidOperationException("Metadata corrections in artist catalog must include a music catalog id.");
        var track = GetOrCreateTrack(musicCatalogId);
        track.Title = @event.Title;
        track.ArtistName = @event.ArtistName;
        track.AlbumTitle = @event.AlbumTitle;
        track.AlbumId = @event.AlbumId;
        track.Isrc = @event.Isrc;
        track.Mbid = @event.Mbid;
        track.DurationMs = @event.DurationMs;
        track.ReleaseDate = @event.ReleaseDate;
        track.UpdatedAt = @event.CorrectedAt;
    }

    private void EnsureArtistMatch(ArtistId expectedArtistId)
    {
        if (artistId is not null && artistId != expectedArtistId)
        {
            throw new InvalidOperationException("Artist catalog id does not match fetched catalog metadata.");
        }
    }

    private ArtistId RequireArtistId() =>
        artistId ?? throw new InvalidOperationException("Artist catalog id has not been established.");

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