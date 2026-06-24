using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;

public sealed class MusicTrackCatalogProjection
{
    private readonly EventHandlers<MusicTrackCatalogProjection> eventHandlers;

    public MusicTrackCatalogProjection(MusicCatalogId musicCatalogId)
    {
        MusicCatalogId = musicCatalogId;
        eventHandlers = CreateHandlers();
        Track = EmptyTrack(musicCatalogId);
    }

    public MusicCatalogId MusicCatalogId { get; }

    public CatalogTrackProjection Track { get; private set; }

    public CatalogArtistProjection? Artist { get; private set; }

    public CatalogAlbumProjection? Album { get; private set; }

    public int ProjectionVersion { get; private set; }

    public static MusicTrackCatalogProjection Load(MusicTrackCatalogProjectionSnapshot snapshot) =>
        new(snapshot.MusicCatalogId)
        {
            Track = snapshot.Track,
            Artist = snapshot.Artist,
            Album = snapshot.Album,
            ProjectionVersion = snapshot.ProjectionVersion
        };

    public MusicTrackCatalogProjectionSnapshot ToSnapshot() =>
        new(
            MusicCatalogId,
            Track,
            Artist,
            Album,
            ProjectionVersion);

    public void Apply(IMusicTrackEvent @event, int version)
    {
        eventHandlers.Handle(@event);
        ProjectionVersion = version;
    }

    private EventHandlers<MusicTrackCatalogProjection> CreateHandlers()
    {
        var handlers = new EventHandlers<MusicTrackCatalogProjection>();

        handlers.Register<TrackDiscovered>(@event =>
        {
            Track = Track with
            {
                Title = @event.Title,
                NormalizedTitle = Normalize(@event.Title),
                ArtistName = string.IsNullOrWhiteSpace(Track.ArtistName) ? @event.Artist : Track.ArtistName,
                SearchText = BuildSearchText(@event.Title, string.IsNullOrWhiteSpace(Track.ArtistName) ? @event.Artist : Track.ArtistName),
                MusicBrainzRecordingId = @event.Mbid,
                Isrc = @event.Isrc,
                DurationMs = @event.DurationMs,
                UpdatedAt = @event.ObservedAt
            };
        });

        handlers.Register<ArtistDiscovered>(@event =>
        {
            var artistId = Coalesce(@event.ArtistId, Track.ArtistId);
            var artistName = Coalesce(@event.ArtistName, Track.ArtistName);

            Track = Track with
            {
                ArtistId = artistId,
                ArtistName = artistName,
                SearchText = BuildSearchText(Track.Title, artistName),
                UpdatedAt = @event.ObservedAt
            };

            if (!string.IsNullOrWhiteSpace(artistId))
            {
                Artist ??= EmptyArtist(artistId);
                Artist = Artist with
                {
                    ArtistId = artistId,
                    Name = Coalesce(artistName, Artist.Name),
                    NormalizedName = Normalize(Coalesce(artistName, Artist.Name)),
                    MusicBrainzArtistId = @event.SourceProvider == ProviderName.MusicBrainz
                        ? CoalesceNullable(@event.SourceArtistId, Artist.MusicBrainzArtistId)
                        : Artist.MusicBrainzArtistId,
                    AvailableProviders = MergeProviders(Artist.AvailableProviders, Track.AvailableProviders),
                    TerminallyUnavailableProviders = MergeProviders(Artist.TerminallyUnavailableProviders, Track.TerminallyUnavailableProviders),
                    ArtworkUrl = CoalesceNullable(Track.ArtworkUrl, Artist.ArtworkUrl),
                    UpdatedAt = @event.ObservedAt
                };
            }

            if (Album is not null)
            {
                Album = Album with
                {
                    ArtistId = artistId,
                    ArtistName = Coalesce(artistName, Album.ArtistName),
                    AvailableProviders = MergeProviders(Album.AvailableProviders, Track.AvailableProviders),
                    TerminallyUnavailableProviders = MergeProviders(Album.TerminallyUnavailableProviders, Track.TerminallyUnavailableProviders),
                    UpdatedAt = @event.ObservedAt
                };
            }
        });

        handlers.Register<AlbumDiscovered>(@event =>
        {
            var albumId = Coalesce(@event.AlbumId, Track.AlbumId);
            var albumTitle = Coalesce(@event.AlbumTitle, Track.AlbumName);

            Track = Track with
            {
                AlbumId = albumId,
                AlbumName = albumTitle,
                UpdatedAt = @event.ObservedAt
            };

            if (!string.IsNullOrWhiteSpace(albumId))
            {
                Album ??= EmptyAlbum(albumId);
                Album = Album with
                {
                    AlbumId = albumId,
                    Name = Coalesce(albumTitle, Album.Name),
                    NormalizedName = Normalize(Coalesce(albumTitle, Album.Name)),
                    ArtistId = Coalesce(Track.ArtistId, Album.ArtistId),
                    ArtistName = Coalesce(Track.ArtistName, Album.ArtistName),
                    MusicBrainzReleaseId = @event.SourceProvider == ProviderName.MusicBrainz
                        ? CoalesceNullable(@event.SourceAlbumId, Album.MusicBrainzReleaseId)
                        : Album.MusicBrainzReleaseId,
                    ReleaseDate = @event.ReleaseDate ?? Album.ReleaseDate,
                    AvailableProviders = MergeProviders(Album.AvailableProviders, Track.AvailableProviders),
                    TerminallyUnavailableProviders = MergeProviders(Album.TerminallyUnavailableProviders, Track.TerminallyUnavailableProviders),
                    ArtworkUrl = CoalesceNullable(Track.ArtworkUrl, Album.ArtworkUrl),
                    UpdatedAt = @event.ObservedAt
                };
            }
        });

        handlers.Register<ProviderReferenceDiscovered>(@event =>
        {
            Track = Track with
            {
                AvailableProviders = AddProvider(Track.AvailableProviders, @event.Provider.Value),
                TerminallyUnavailableProviders = RemoveProvider(Track.TerminallyUnavailableProviders, @event.Provider.Value),
                ProviderReferences = UpsertProviderReference(
                    Track.ProviderReferences,
                    new CatalogProviderReferenceProjection(
                        @event.Provider.Value,
                        "track",
                        @event.ExternalId ?? string.Empty,
                        @event.Url.ToString(),
                        @event.ObservedAt)),
                UpdatedAt = @event.ObservedAt
            };

            if (Artist is not null)
            {
                Artist = Artist with
                {
                    AvailableProviders = AddProvider(Artist.AvailableProviders, @event.Provider.Value),
                    TerminallyUnavailableProviders = RemoveProvider(Artist.TerminallyUnavailableProviders, @event.Provider.Value),
                    UpdatedAt = @event.ObservedAt
                };
            }

            if (Album is not null)
            {
                Album = Album with
                {
                    AvailableProviders = AddProvider(Album.AvailableProviders, @event.Provider.Value),
                    TerminallyUnavailableProviders = RemoveProvider(Album.TerminallyUnavailableProviders, @event.Provider.Value),
                    UpdatedAt = @event.ObservedAt
                };
            }
        });

        handlers.Register<ProviderReferenceLookupFailed>(@event =>
        {
            Track = Track with
            {
                TerminallyUnavailableProviders = AddProvider(Track.TerminallyUnavailableProviders, @event.Provider.Value),
                AvailableProviders = RemoveProvider(Track.AvailableProviders, @event.Provider.Value),
                ProviderReferences = RemoveProviderReference(Track.ProviderReferences, @event.Provider.Value),
                UpdatedAt = @event.ObservedAt
            };

            if (Artist is not null)
            {
                Artist = Artist with
                {
                    TerminallyUnavailableProviders = AddProvider(Artist.TerminallyUnavailableProviders, @event.Provider.Value),
                    AvailableProviders = RemoveProvider(Artist.AvailableProviders, @event.Provider.Value),
                    UpdatedAt = @event.ObservedAt
                };
            }

            if (Album is not null)
            {
                Album = Album with
                {
                    TerminallyUnavailableProviders = AddProvider(Album.TerminallyUnavailableProviders, @event.Provider.Value),
                    AvailableProviders = RemoveProvider(Album.AvailableProviders, @event.Provider.Value),
                    UpdatedAt = @event.ObservedAt
                };
            }
        });

        handlers.Register<ArtworkDiscovered>(@event =>
        {
            switch (@event.EntityKind)
            {
                case CatalogEntityKind.Track:
                    Track = Track with
                    {
                        ArtworkUrl = @event.Url.ToString(),
                        UpdatedAt = @event.ObservedAt
                    };
                    break;
                case CatalogEntityKind.Artist:
                    if (!string.IsNullOrWhiteSpace(@event.EntityId))
                    {
                        Artist ??= EmptyArtist(@event.EntityId);
                    }

                    if (Artist is not null)
                    {
                        Artist = Artist with
                        {
                            ArtworkUrl = @event.Url.ToString(),
                            UpdatedAt = @event.ObservedAt
                        };
                    }

                    break;
                case CatalogEntityKind.Album:
                    if (!string.IsNullOrWhiteSpace(@event.EntityId))
                    {
                        Album ??= EmptyAlbum(@event.EntityId);
                    }

                    if (Album is not null)
                    {
                        Album = Album with
                        {
                            ArtworkUrl = @event.Url.ToString(),
                            UpdatedAt = @event.ObservedAt
                        };
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(@event.EntityKind), @event.EntityKind, null);
            }
        });

        handlers.Register<MetadataCorrected>(@event =>
        {
            Track = Track with
            {
                Title = @event.Title,
                NormalizedTitle = Normalize(@event.Title),
                ArtistName = @event.ArtistName,
                AlbumName = @event.AlbumTitle ?? Track.AlbumName,
                ArtistId = @event.ArtistId ?? Track.ArtistId,
                AlbumId = @event.AlbumId ?? Track.AlbumId,
                MusicBrainzRecordingId = @event.Mbid,
                Isrc = @event.Isrc,
                DurationMs = @event.DurationMs,
                SearchText = BuildSearchText(@event.Title, @event.ArtistName),
                UpdatedAt = @event.CorrectedAt
            };

            if (!string.IsNullOrWhiteSpace(Track.ArtistId))
            {
                Artist ??= EmptyArtist(Track.ArtistId);
                Artist = Artist with
                {
                    ArtistId = Track.ArtistId,
                    Name = @event.ArtistName,
                    NormalizedName = Normalize(@event.ArtistName),
                    MusicBrainzArtistId = CoalesceNullable(@event.SourceArtistId, Artist.MusicBrainzArtistId),
                    AvailableProviders = MergeProviders(Artist.AvailableProviders, Track.AvailableProviders),
                    TerminallyUnavailableProviders = MergeProviders(Artist.TerminallyUnavailableProviders, Track.TerminallyUnavailableProviders),
                    ArtworkUrl = CoalesceNullable(Track.ArtworkUrl, Artist.ArtworkUrl),
                    UpdatedAt = @event.CorrectedAt
                };
            }

            if (!string.IsNullOrWhiteSpace(Track.AlbumId))
            {
                Album ??= EmptyAlbum(Track.AlbumId);
                Album = Album with
                {
                    AlbumId = Track.AlbumId,
                    Name = Track.AlbumName,
                    NormalizedName = Normalize(Track.AlbumName),
                    ArtistId = Track.ArtistId,
                    ArtistName = Track.ArtistName,
                    MusicBrainzReleaseId = CoalesceNullable(@event.SourceAlbumId, Album.MusicBrainzReleaseId),
                    ReleaseDate = @event.ReleaseDate ?? Album.ReleaseDate,
                    AvailableProviders = MergeProviders(Album.AvailableProviders, Track.AvailableProviders),
                    TerminallyUnavailableProviders = MergeProviders(Album.TerminallyUnavailableProviders, Track.TerminallyUnavailableProviders),
                    ArtworkUrl = CoalesceNullable(Track.ArtworkUrl, Album.ArtworkUrl),
                    UpdatedAt = @event.CorrectedAt
                };
            }
        });

        handlers.Register<PlaybackReferencesResolutionRequired>(_ => { });
        return handlers;
    }

    private static CatalogTrackProjection EmptyTrack(MusicCatalogId musicCatalogId) =>
        new(
            musicCatalogId.Value,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            null,
            null,
            null,
            [],
            [],
            [],
            null,
            default);

    private static CatalogArtistProjection EmptyArtist(string artistId) =>
        new(
            artistId,
            string.Empty,
            string.Empty,
            null,
            [],
            [],
            null,
            default);

    private static CatalogAlbumProjection EmptyAlbum(string albumId) =>
        new(
            albumId,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            null,
            [],
            [],
            null,
            null,
            default);

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static string BuildSearchText(string title, string artistName) =>
        $"{title} {artistName}".Trim().ToLowerInvariant();

    private static string Coalesce(string? preferred, string fallback) =>
        string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;

    private static string? CoalesceNullable(string? preferred, string? fallback) =>
        string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;

    private static IReadOnlyList<string> MergeProviders(
        IReadOnlyList<string> current,
        IReadOnlyList<string> additional) =>
        current.Concat(additional).Distinct(StringComparer.Ordinal).ToArray();

    private static IReadOnlyList<string> AddProvider(
        IReadOnlyList<string> providers,
        string provider) =>
        providers.Contains(provider, StringComparer.Ordinal)
            ? providers.ToArray()
            : providers.Concat([provider]).ToArray();

    private static IReadOnlyList<string> RemoveProvider(
        IReadOnlyList<string> providers,
        string provider) =>
        providers.Where(x => !string.Equals(x, provider, StringComparison.Ordinal)).ToArray();

    private static IReadOnlyList<CatalogProviderReferenceProjection> UpsertProviderReference(
        IReadOnlyList<CatalogProviderReferenceProjection> providers,
        CatalogProviderReferenceProjection reference) =>
        providers
            .Where(x => !string.Equals(x.Provider, reference.Provider, StringComparison.Ordinal))
            .Append(reference)
            .ToArray();

    private static IReadOnlyList<CatalogProviderReferenceProjection> RemoveProviderReference(
        IReadOnlyList<CatalogProviderReferenceProjection> providers,
        string provider) =>
        providers.Where(x => !string.Equals(x.Provider, provider, StringComparison.Ordinal)).ToArray();
}
