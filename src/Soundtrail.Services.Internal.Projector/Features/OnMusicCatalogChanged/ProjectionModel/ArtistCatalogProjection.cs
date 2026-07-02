using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;

public sealed class ArtistCatalogProjection
{
    private readonly EventHandlers<ArtistCatalogProjection> eventHandlers;
    private readonly Dictionary<string, AlbumState> albums = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TrackState> tracks = new(StringComparer.Ordinal);
    private string? artistName;
    private string? sourceArtistId;
    private string? artworkUrl;

    public ArtistCatalogProjection(ArtistId artistId)
    {
        ArtistId = artistId;
        eventHandlers = CreateHandlers();
    }

    public ArtistId ArtistId { get; }

    public int Version { get; private set; }

    public static ArtistCatalogProjection Replay(
        ArtistId artistId,
        IReadOnlyList<VersionedCatalogEvent> events)
    {
        var projection = new ArtistCatalogProjection(artistId);
        foreach (var @event in events.OrderBy(x => x.Version))
        {
            projection.Apply(@event.Event, @event.Version);
        }

        return projection;
    }

    public CatalogArtistRecordDto? ArtistDocument =>
        string.IsNullOrWhiteSpace(artistName)
            ? null
            : new CatalogArtistRecordDto
            {
                Id = CatalogArtistRecordDto.GetDocumentId(ArtistId.Value),
                ArtistId = ArtistId.Value,
                Name = artistName,
                NormalizedName = NormalizeFreeText(artistName),
                SearchText = NormalizeFreeText(artistName),
                MusicBrainzArtistId = sourceArtistId,
                AvailableProviders = tracks.Values
                    .SelectMany(track => track.ProviderReferences.Keys)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray(),
                TerminallyUnavailableProviders = tracks.Values
                    .SelectMany(track => track.FailedProviders)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray(),
                ArtworkUrl = artworkUrl,
                UpdatedAt = tracks.Values.Select(x => x.UpdatedAt).Append(DateTimeOffset.MinValue).Max()
            };

    public IReadOnlyList<CatalogAlbumRecordDto> AlbumDocuments =>
        albums.Values
            .Select(album => new CatalogAlbumRecordDto
            {
                Id = CatalogAlbumRecordDto.GetDocumentId(album.AlbumId.Value),
                ArtistId = ArtistId.Value,
                AlbumId = album.AlbumId.Value,
                Name = album.AlbumTitle ?? string.Empty,
                NormalizedName = NormalizeFreeText(album.AlbumTitle),
                ArtistName = artistName ?? string.Empty,
                SearchText = NormalizeFreeText($"{album.AlbumTitle} {artistName}".Trim()),
                MusicBrainzReleaseId = album.SourceAlbumId,
                AvailableProviders = tracks.Values
                    .Where(track => string.Equals(track.AlbumId, album.AlbumId.Value, StringComparison.Ordinal))
                    .SelectMany(track => track.ProviderReferences.Keys)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray(),
                TerminallyUnavailableProviders = tracks.Values
                    .Where(track => string.Equals(track.AlbumId, album.AlbumId.Value, StringComparison.Ordinal))
                    .SelectMany(track => track.FailedProviders)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray(),
                ArtworkUrl = album.ArtworkUrl,
                ReleaseDate = album.ReleaseDate,
                UpdatedAt = tracks.Values
                    .Where(track => string.Equals(track.AlbumId, album.AlbumId.Value, StringComparison.Ordinal))
                    .Select(track => track.UpdatedAt)
                    .Append(album.UpdatedAt)
                    .Max()
            })
            .OrderBy(x => x.AlbumId, StringComparer.Ordinal)
            .ToArray();

    public IReadOnlyList<CatalogTrackRecordDto> TrackDocuments =>
        tracks.Values
            .Select(track => new CatalogTrackRecordDto
            {
                Id = CatalogTrackRecordDto.GetDocumentId(track.MusicCatalogId.Value),
                TrackId = track.MusicCatalogId.Value,
                ArtistId = ArtistId.Value,
                AlbumId = track.AlbumId ?? string.Empty,
                Title = track.Title,
                NormalizedTitle = NormalizeFreeText(track.Title),
                ArtistName = track.ArtistName,
                AlbumName = track.AlbumTitle ?? string.Empty,
                SearchText = NormalizeFreeText($"{track.Title} {track.ArtistName}".Trim()),
                MusicBrainzRecordingId = track.Mbid,
                Isrc = track.Isrc,
                DurationMs = track.DurationMs,
                AvailableProviders = track.ProviderReferences.Keys
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray(),
                TerminallyUnavailableProviders = track.FailedProviders
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray(),
                ProviderReferences = track.ProviderReferences.Values
                    .Select(reference => new CatalogProviderReferenceRecordDto
                    {
                        Provider = reference.Provider.Value,
                        ProviderEntityType = "track",
                        ProviderId = reference.ExternalId ?? string.Empty,
                        Url = reference.Url.ToString(),
                        DiscoveredAt = reference.ObservedAt
                    })
                    .OrderBy(x => x.Provider, StringComparer.Ordinal)
                    .ToArray(),
                ArtworkUrl = track.ArtworkUrl,
                UpdatedAt = track.UpdatedAt
            })
            .OrderBy(x => x.TrackId, StringComparer.Ordinal)
            .ToArray();

    public IReadOnlyList<RavenTrackRecordDto> RavenTrackDocuments =>
        tracks.Values
            .Select(track => new RavenTrackRecordDto
            {
                Id = RavenTrackRecordDto.GetDocumentId(track.MusicCatalogId.Value),
                ArtistId = ArtistId.Value,
                AlbumId = track.AlbumId,
                Title = track.Title,
                Artist = track.ArtistName,
                NormalizedArtist = NormalizeFreeText(track.ArtistName),
                AlbumTitle = track.AlbumTitle,
                NormalizedAlbumTitle = NormalizeFreeText(track.AlbumTitle),
                SearchText = RavenTrackRecordDto.BuildSearchText(track.Title, track.ArtistName),
                Isrc = track.Isrc,
                NormalizedIsrc = NormalizeCompact(track.Isrc),
                Mbid = track.Mbid,
                NormalizedMbid = NormalizeCompact(track.Mbid),
                AppleId = track.ProviderReferences.Values.FirstOrDefault(x => x.Provider == ProviderName.AppleMusic)?.ExternalId,
                SpotifyId = track.ProviderReferences.Values.FirstOrDefault(x => x.Provider == ProviderName.Spotify)?.ExternalId,
                DurationMs = track.DurationMs,
                ReleaseDate = track.ReleaseDate,
                ArtworkUrl = track.ArtworkUrl,
                ResolvedMetadata = new RavenSongMetadataRecordDto
                {
                    Title = track.Title,
                    Artist = track.ArtistName,
                    Isrc = track.Isrc,
                    Mbid = track.Mbid,
                    DurationMs = track.DurationMs
                },
                AppleReference = ToProviderReference(track, ProviderName.AppleMusic),
                YouTubeMusicReference = ToProviderReference(track, ProviderName.YoutubeMusic),
                IsPlayable = track.ProviderReferences.ContainsKey(ProviderName.AppleMusic.Value)
                             || track.ProviderReferences.ContainsKey(ProviderName.YoutubeMusic.Value)
                             || track.ProviderReferences.ContainsKey(ProviderName.Spotify.Value),
                ProjectionVersion = Version
            })
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToArray();

    public CatalogProjectionCheckpointDocument CheckpointDocument =>
        new()
        {
            Id = CatalogProjectionCheckpointDocument.GetDocumentId(ArtistId.Value),
            ArtistId = ArtistId.Value,
            LastAppliedVersion = Version,
            UpdatedAt = tracks.Values.Select(x => x.UpdatedAt).Append(DateTimeOffset.UtcNow).Max()
        };

    public void Apply(IDomainEvent @event, int version)
    {
        eventHandlers.Handle(@event);
        Version = version;
    }

    private EventHandlers<ArtistCatalogProjection> CreateHandlers()
    {
        var handlers = new EventHandlers<ArtistCatalogProjection>();

        handlers.Register<ArtistDiscovered>(@event =>
        {
            artistName = @event.ArtistName;
            sourceArtistId = @event.SourceArtistId;

             if (string.IsNullOrWhiteSpace(artworkUrl)
                 && TryGetSingleLegacyTrack(out var track)
                 && !string.IsNullOrWhiteSpace(track.ArtworkUrl))
             {
                 artworkUrl = track.ArtworkUrl;
             }
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

            if (TryGetSingleLegacyTrack(out var track) && string.IsNullOrWhiteSpace(track.AlbumId))
            {
                track.AlbumId = albumId;
                track.AlbumTitle ??= @event.AlbumTitle;
                track.UpdatedAt = @event.ObservedAt;

                if (albums.TryGetValue(albumId, out var repairedAlbum) && string.IsNullOrWhiteSpace(repairedAlbum.ArtworkUrl))
                {
                    repairedAlbum.ArtworkUrl = track.ArtworkUrl;
                }
            }
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
                case CatalogEntityKind.Track:
                    if (TryGetSingleLegacyTrack(out var inferredTrack))
                    {
                        inferredTrack.ArtworkUrl = @event.Url.ToString();
                        inferredTrack.UpdatedAt = @event.ObservedAt;
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

            if (!string.IsNullOrWhiteSpace(@event.ArtistName))
            {
                artistName = @event.ArtistName;
            }

            if (!string.IsNullOrWhiteSpace(@event.SourceArtistId))
            {
                sourceArtistId = @event.SourceArtistId;
            }

            if (!string.IsNullOrWhiteSpace(@event.AlbumId))
            {
                albums[@event.AlbumId] = new AlbumState(
                    AlbumId.From(@event.AlbumId),
                    @event.AlbumTitle,
                    @event.SourceAlbumId,
                    @event.ReleaseDate,
                    albums.TryGetValue(@event.AlbumId, out var existingAlbum) ? existingAlbum.ArtworkUrl : track.ArtworkUrl,
                    @event.CorrectedAt);
            }
        });

        return handlers;
    }

    private TrackState GetOrCreateTrack(MusicCatalogId musicCatalogId)
    {
        if (!tracks.TryGetValue(musicCatalogId.Value, out var track))
        {
            track = new TrackState(musicCatalogId);
            tracks[musicCatalogId.Value] = track;
        }

        return track;
    }

    private bool TryGetSingleLegacyTrack(out TrackState track)
    {
        if (tracks.Count == 1)
        {
            track = tracks.Values.Single();
            return true;
        }

        track = null!;
        return false;
    }

    private static RavenProviderReferenceRecordDto? ToProviderReference(
        TrackState track,
        ProviderName provider)
    {
        return track.ProviderReferences.TryGetValue(provider.Value, out var reference)
            ? new RavenProviderReferenceRecordDto
            {
                Provider = reference.Provider.Value,
                Url = reference.Url.ToString(),
                ExternalId = reference.ExternalId,
                SourceProvider = reference.SourceProvider.Value
            }
            : null;
    }

    private static string NormalizeFreeText(string? value) =>
        MusicIdentityText.NormalizeFreeText(value ?? string.Empty);

    private static string NormalizeCompact(string? value) =>
        MusicIdentityText.NormalizeCompact(value ?? string.Empty);

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
