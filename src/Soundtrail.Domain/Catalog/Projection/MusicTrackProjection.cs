using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Events;

namespace Soundtrail.Domain.Catalog.Projection;

public sealed class MusicTrackProjection
{
    private readonly EventHandlers<MusicTrackProjection> eventHandlers;

    public MusicTrackProjection()
    {
        this.eventHandlers = CreateHandlers();
        Artist = ArtistName.Empty;
        AlbumTitle = AlbumTitle.Empty;
    }

    public string? ArtistId { get; private set; }

    public string? AlbumId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public ArtistName Artist { get; private set; }

    public AlbumTitle AlbumTitle { get; private set; }

    public string SearchText { get; private set; } = string.Empty;

    public string? Isrc { get; private set; }

    public string NormalizedIsrc { get; private set; } = string.Empty;

    public string? Mbid { get; private set; }

    public string NormalizedMbid { get; private set; } = string.Empty;

    public string? AppleId { get; private set; }

    public string? SpotifyId { get; private set; }

    public int? DurationMs { get; private set; }

    public DateOnly? ReleaseDate { get; private set; }

    public string? ArtworkUrl { get; private set; }

    public ProjectedSongMetadata? ResolvedMetadata { get; private set; }

    public ProjectedProviderReference? AppleReference { get; private set; }

    public ProjectedProviderReference? YouTubeMusicReference { get; private set; }

    public bool IsPlayable { get; private set; }

    public int ProjectionVersion { get; private set; }

    public static MusicTrackProjection Load(MusicTrackProjectionSnapshot snapshot) =>
        new()
        {
            ArtistId = snapshot.ArtistId,
            AlbumId = snapshot.AlbumId,
            Title = snapshot.Title,
            Artist = snapshot.Artist,
            AlbumTitle = snapshot.AlbumTitle,
            SearchText = snapshot.SearchText,
            Isrc = snapshot.Isrc,
            NormalizedIsrc = snapshot.NormalizedIsrc,
            Mbid = snapshot.Mbid,
            NormalizedMbid = snapshot.NormalizedMbid,
            AppleId = snapshot.AppleId,
            SpotifyId = snapshot.SpotifyId,
            DurationMs = snapshot.DurationMs,
            ReleaseDate = snapshot.ReleaseDate,
            ArtworkUrl = snapshot.ArtworkUrl,
            ResolvedMetadata = snapshot.ResolvedMetadata,
            AppleReference = snapshot.AppleReference,
            YouTubeMusicReference = snapshot.YouTubeMusicReference,
            IsPlayable = snapshot.IsPlayable,
            ProjectionVersion = snapshot.ProjectionVersion
        };

    public MusicTrackProjectionSnapshot ToSnapshot() =>
        new(
            ArtistId,
            AlbumId,
            Title,
            Artist,
            AlbumTitle,
            SearchText,
            Isrc,
            NormalizedIsrc,
            Mbid,
            NormalizedMbid,
            AppleId,
            SpotifyId,
            DurationMs,
            ReleaseDate,
            ArtworkUrl,
            ResolvedMetadata,
            AppleReference,
            YouTubeMusicReference,
            IsPlayable,
            ProjectionVersion);

    public void Replay(IReadOnlyList<IMusicTrackEvent> events)
    {
        for (var index = 0; index < events.Count; index++)
        {
            Apply(events[index], index + 1);
        }
    }

    public void Apply(IMusicTrackEvent @event, int version)
    {
        this.eventHandlers.Handle(@event);
        IsPlayable =
            ResolvedMetadata is not null
            && (AppleReference is not null
                || YouTubeMusicReference is not null
                || !string.IsNullOrWhiteSpace(SpotifyId));
        ProjectionVersion = version;
    }

    private EventHandlers<MusicTrackProjection> CreateHandlers()
    {
        var handlers = new EventHandlers<MusicTrackProjection>();

        handlers.Register<TrackDiscovered>(@event =>
        {
            var artistName = ArtistName.From(@event.Artist);
            ResolvedMetadata = new ProjectedSongMetadata(
                @event.Title,
                artistName,
                @event.Isrc,
                NormalizeCompact(@event.Isrc),
                @event.Mbid,
                NormalizeCompact(@event.Mbid),
                @event.DurationMs);
            Title = @event.Title;
            Artist = artistName;
            Isrc = @event.Isrc;
            NormalizedIsrc = NormalizeCompact(@event.Isrc);
            Mbid = @event.Mbid;
            NormalizedMbid = NormalizeCompact(@event.Mbid);
            DurationMs = @event.DurationMs;
            SearchText = BuildSearchText(Title, Artist);
        });

        handlers.Register<ProviderReferenceDiscovered>(@event =>
        {
            var reference = new ProjectedProviderReference(
                @event.Provider,
                @event.Url,
                @event.ExternalId,
                @event.SourceProvider);

            switch (@event.Provider.Value)
            {
                case "AppleMusic":
                    AppleReference = reference;
                    AppleId = @event.ExternalId;
                    break;
                case "YoutubeMusic":
                    YouTubeMusicReference = reference;
                    break;
                case "Spotify":
                    SpotifyId = @event.ExternalId;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(@event.Provider), @event.Provider, null);
            }
        });

        handlers.Register<ProviderReferenceLookupFailed>(_ => { });
        handlers.Register<StreamingLocationsRequired>(_ => { });

        handlers.Register<AlbumDiscovered>(@event =>
        {
            AlbumId = @event.AlbumId;
            AlbumTitle = AlbumTitle.From(@event.AlbumTitle);
            ReleaseDate = @event.ReleaseDate ?? ReleaseDate;
        });

        handlers.Register<ArtistDiscovered>(@event =>
        {
            ArtistId = @event.ArtistId;
            if (!string.IsNullOrWhiteSpace(@event.ArtistName))
            {
                Artist = ArtistName.From(@event.ArtistName);
                SearchText = BuildSearchText(Title, Artist);
            }
        });

        handlers.Register<ArtworkDiscovered>(@event =>
        {
            if (@event.EntityKind == CatalogEntityKind.Track)
            {
                ArtworkUrl = @event.Url.ToString();
            }
        });

        handlers.Register<MetadataCorrected>(@event =>
        {
            var artistName = ArtistName.From(@event.ArtistName);
            ResolvedMetadata = new ProjectedSongMetadata(
                @event.Title,
                artistName,
                @event.Isrc,
                NormalizeCompact(@event.Isrc),
                @event.Mbid,
                NormalizeCompact(@event.Mbid),
                @event.DurationMs);
            Title = @event.Title;
            Artist = artistName;
            ArtistId = @event.ArtistId;
            AlbumTitle = AlbumTitle.From(@event.AlbumTitle);
            AlbumId = @event.AlbumId;
            Isrc = @event.Isrc;
            NormalizedIsrc = NormalizeCompact(@event.Isrc);
            Mbid = @event.Mbid;
            NormalizedMbid = NormalizeCompact(@event.Mbid);
            DurationMs = @event.DurationMs;
            ReleaseDate = @event.ReleaseDate ?? ReleaseDate;
            SearchText = BuildSearchText(Title, Artist);
        });

        return handlers;
    }

    private static string BuildSearchText(string title, ArtistName artist) =>
        ArtistName.From($"{title} {artist.Value}".Trim()).Normalized;

    private static string NormalizeCompact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }
}
