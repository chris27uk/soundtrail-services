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
    private readonly Dictionary<string, AlbumState> albums = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TrackState> tracks = new(StringComparer.Ordinal);
    private ArtistId? artistId;
    private string? artistName;
    private string? sourceArtistId;
    private string? artworkUrl;

    public ArtistCatalog()
    {
        eventHandlers = CreateHandlers();
    }

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

    public void Replay(IDomainEvent @event) => Apply(@event, isNew: false);

    public void ArtistMetadataFetched(ArtistMetadataFetched fetched)
    {
        DiscoverArtist(
            fetched.ArtistId,
            fetched.Metadata.ArtistName,
            fetched.Metadata.SourceArtistId,
            fetched.SourceProvider,
            fetched.CreatedAt);
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

    public void TrackMetadataFetched(
        ArtistId resolvedArtistId,
        MusicCatalogMetadataFetched fetched)
    {
        EnsureArtistMatch(resolvedArtistId);

        if (fetched.Metadata is not null && fetched.SourceProvider == LookupSource.MusicBrainz)
        {
            DiscoverArtist(
                resolvedArtistId,
                fetched.Metadata.Artist,
                fetched.Metadata.SourceArtistId,
                fetched.SourceProvider,
                fetched.CreatedAt);

            if (fetched.Hierarchy?.AlbumId is not null)
            {
                DiscoverAlbum(
                    resolvedArtistId,
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
                        resolvedArtistId,
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

    public ArtistCatalogArtistView? GetArtist() =>
        artistId is null || string.IsNullOrWhiteSpace(artistName)
            ? null
            : new ArtistCatalogArtistView(
                artistId.Value,
                artistName,
                sourceArtistId,
                artworkUrl,
                tracks.Values
                    .SelectMany(track => track.ProviderReferences.Keys)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray(),
                tracks.Values
                    .SelectMany(track => track.FailedProviders)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray(),
                tracks.Values
                    .Select(track => track.UpdatedAt)
                    .Append(DateTimeOffset.MinValue)
                    .Max());

    public IReadOnlyList<ArtistCatalogAlbumView> GetAlbums() =>
        albums.Values
            .Select(album => new ArtistCatalogAlbumView(
                album.AlbumId,
                RequireArtistId(),
                album.AlbumTitle ?? string.Empty,
                artistName ?? string.Empty,
                album.SourceAlbumId,
                album.ReleaseDate,
                album.ArtworkUrl,
                tracks.Values
                    .Where(track => string.Equals(track.AlbumId, album.AlbumId.Value, StringComparison.Ordinal))
                    .SelectMany(track => track.ProviderReferences.Keys)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray(),
                tracks.Values
                    .Where(track => string.Equals(track.AlbumId, album.AlbumId.Value, StringComparison.Ordinal))
                    .SelectMany(track => track.FailedProviders)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray(),
                tracks.Values
                    .Where(track => string.Equals(track.AlbumId, album.AlbumId.Value, StringComparison.Ordinal))
                    .Select(track => track.UpdatedAt)
                    .Append(album.UpdatedAt)
                    .Max()))
            .OrderBy(x => x.AlbumId.Value, StringComparer.Ordinal)
            .ToArray();

    public IReadOnlyList<ArtistCatalogTrackView> GetTracks() =>
        tracks.Values
            .Select(track => new ArtistCatalogTrackView(
                track.MusicCatalogId,
                RequireArtistId(),
                track.AlbumId is null ? null : AlbumId.From(track.AlbumId),
                track.Title,
                track.ArtistName,
                track.AlbumTitle,
                track.Isrc,
                track.Mbid,
                track.DurationMs,
                track.ReleaseDate,
                track.ArtworkUrl,
                track.ProviderReferences.Values
                    .Select(reference => new ArtistCatalogTrackProviderReferenceView(
                        reference.Provider,
                        reference.ExternalId,
                        reference.Url,
                        reference.SourceProvider,
                        reference.ObservedAt))
                    .OrderBy(x => x.Provider.Value, StringComparer.Ordinal)
                    .ToArray(),
                track.FailedProviders
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray(),
                track.StreamingLocationsRequired,
                track.UpdatedAt))
            .OrderBy(x => x.MusicCatalogId.Value, StringComparer.Ordinal)
            .ToArray();

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
        handlers.Register<ArtistDiscovered>(@event =>
        {
            artistId ??= ArtistId.From(@event.ArtistId ?? throw new InvalidOperationException("Artist id is required."));
            artistName = @event.ArtistName;
            sourceArtistId = @event.SourceArtistId;
        });
        handlers.Register<AlbumDiscovered>(@event =>
        {
            var albumId = @event.AlbumId ?? throw new InvalidOperationException("Album id is required.");
            albums[albumId] = new AlbumState(
                AlbumId.From(albumId),
                @event.AlbumTitle,
                @event.SourceAlbumId,
                @event.ReleaseDate,
                albums.TryGetValue(albumId, out var existing) ? existing.ArtworkUrl : null,
                @event.ObservedAt);
        });
        handlers.Register<TrackDiscovered>(@event =>
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
        });
        handlers.Register<ProviderReferenceDiscovered>(@event =>
        {
            var musicCatalogId = @event.MusicCatalogId
                                 ?? throw new InvalidOperationException("Provider reference facts in artist catalog must include a music catalog id.");
            var track = GetOrCreateTrack(musicCatalogId);
            track.ProviderReferences[@event.Provider.Value] = new ProviderReferenceState(
                @event.Provider,
                @event.ExternalId,
                @event.Url,
                @event.SourceProvider,
                @event.ObservedAt);
            track.FailedProviders.Remove(@event.Provider.Value);
            track.UpdatedAt = @event.ObservedAt;
        });
        handlers.Register<ProviderReferenceLookupFailed>(@event =>
        {
            var musicCatalogId = @event.MusicCatalogId
                                 ?? throw new InvalidOperationException("Provider reference failure facts in artist catalog must include a music catalog id.");
            var track = GetOrCreateTrack(musicCatalogId);
            track.ProviderReferences.Remove(@event.Provider.Value);
            track.FailedProviders.Add(@event.Provider.Value);
            track.UpdatedAt = @event.ObservedAt;
        });
        handlers.Register<StreamingLocationsRequired>(@event =>
        {
            var track = GetOrCreateTrack(@event.MusicCatalogId);
            track.StreamingLocationsRequired = true;
            track.AlbumId ??= @event.Hierarchy?.AlbumId?.Value;
            track.UpdatedAt = @event.ObservedAt;
        });
        handlers.Register<ArtworkDiscovered>(@event =>
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
        });
        handlers.Register<MetadataCorrected>(@event =>
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
        });
        return handlers;
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

    private TrackState GetOrCreateTrack(MusicCatalogId musicCatalogId)
    {
        if (!tracks.TryGetValue(musicCatalogId.Value, out var track))
        {
            track = new TrackState(musicCatalogId);
            tracks[musicCatalogId.Value] = track;
        }

        return track;
    }

    private sealed class AlbumState
    {
        public AlbumState(
            AlbumId albumId,
            string? albumTitle,
            string? sourceAlbumId,
            DateOnly? releaseDate,
            string? artworkUrl,
            DateTimeOffset updatedAt)
        {
            AlbumId = albumId;
            AlbumTitle = albumTitle;
            SourceAlbumId = sourceAlbumId;
            ReleaseDate = releaseDate;
            ArtworkUrl = artworkUrl;
            UpdatedAt = updatedAt;
        }

        public AlbumId AlbumId { get; }

        public string? AlbumTitle { get; }

        public string? SourceAlbumId { get; }

        public DateOnly? ReleaseDate { get; }

        public string? ArtworkUrl { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }

    private sealed class TrackState
    {
        public TrackState(MusicCatalogId musicCatalogId)
        {
            MusicCatalogId = musicCatalogId;
        }

        public MusicCatalogId MusicCatalogId { get; }

        public string Title { get; set; } = string.Empty;

        public string ArtistName { get; set; } = string.Empty;

        public string? AlbumTitle { get; set; }

        public string? AlbumId { get; set; }

        public int? DurationMs { get; set; }

        public string? Isrc { get; set; }

        public string? Mbid { get; set; }

        public DateOnly? ReleaseDate { get; set; }

        public string? ArtworkUrl { get; set; }

        public bool StreamingLocationsRequired { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public Dictionary<string, ProviderReferenceState> ProviderReferences { get; } = new(StringComparer.Ordinal);

        public HashSet<string> FailedProviders { get; } = new(StringComparer.Ordinal);
    }

    private sealed class ProviderReferenceState
    {
        public ProviderReferenceState(
            ProviderName provider,
            string? externalId,
            Uri url,
            LookupSource sourceProvider,
            DateTimeOffset observedAt)
        {
            Provider = provider;
            ExternalId = externalId;
            Url = url;
            SourceProvider = sourceProvider;
            ObservedAt = observedAt;
        }

        public ProviderName Provider { get; }

        public string? ExternalId { get; }

        public Uri Url { get; }

        public LookupSource SourceProvider { get; }

        public DateTimeOffset ObservedAt { get; }
    }
}

public sealed record ArtistCatalogArtistView(
    ArtistId ArtistId,
    string Name,
    string? SourceArtistId,
    string? ArtworkUrl,
    IReadOnlyList<string> AvailableProviders,
    IReadOnlyList<string> TerminallyUnavailableProviders,
    DateTimeOffset UpdatedAt);

public sealed record ArtistCatalogAlbumView(
    AlbumId AlbumId,
    ArtistId ArtistId,
    string Name,
    string ArtistName,
    string? SourceAlbumId,
    DateOnly? ReleaseDate,
    string? ArtworkUrl,
    IReadOnlyList<string> AvailableProviders,
    IReadOnlyList<string> TerminallyUnavailableProviders,
    DateTimeOffset UpdatedAt);

public sealed record ArtistCatalogTrackView(
    MusicCatalogId MusicCatalogId,
    ArtistId ArtistId,
    AlbumId? AlbumId,
    string Title,
    string ArtistName,
    string? AlbumTitle,
    string? Isrc,
    string? Mbid,
    int? DurationMs,
    DateOnly? ReleaseDate,
    string? ArtworkUrl,
    IReadOnlyList<ArtistCatalogTrackProviderReferenceView> ProviderReferences,
    IReadOnlyList<string> TerminallyUnavailableProviders,
    bool StreamingLocationsRequired,
    DateTimeOffset UpdatedAt);

public sealed record ArtistCatalogTrackProviderReferenceView(
    ProviderName Provider,
    string? ExternalId,
    Uri Url,
    LookupSource SourceProvider,
    DateTimeOffset ObservedAt);
