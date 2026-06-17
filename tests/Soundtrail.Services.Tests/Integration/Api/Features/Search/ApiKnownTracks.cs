using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.CatalogBrowsing;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Tests.Integration.Api.Features.Search
{
    internal static class ApiKnownTracks
    {
        public static SearchCatalogResult MrBrightsideCatalogTrack() =>
            new(
                SearchResultType.Track,
                "track_mr_brightside",
                "Mr. Brightside",
                "artist_the_killers",
                "The Killers",
                "album_hot_fuss",
                "Hot Fuss",
                PlayabilityStatus.Playable,
                [ProviderName.Spotify, ProviderName.AppleMusic],
                [],
                [new ProviderReference(
                    ProviderName.Spotify,
                    "track",
                    "spotify-track-1",
                    new Uri("https://open.spotify.com/track/spotify-track-1"),
                    new DateTimeOffset(2026, 6, 17, 12, 0, 0, TimeSpan.Zero))]);

        public static SearchCatalogResult TheKillersArtist() =>
            new(
                SearchResultType.Artist,
                "artist_the_killers",
                "The Killers",
                "artist_the_killers",
                "The Killers",
                null,
                null,
                PlayabilityStatus.Playable,
                [ProviderName.Spotify, ProviderName.AppleMusic],
                [],
                []);

        public static ArtistDetailsResponse TheKillersArtistDetails() =>
            new(
                ArtistId.From("artist_the_killers"),
                "The Killers",
                [new AlbumSummary(
                    AlbumId.From("album_hot_fuss"),
                    "Hot Fuss",
                    new DateOnly(2004, 6, 7),
                PlayabilityStatus.Playable,
                [ProviderName.Spotify, ProviderName.AppleMusic],
                [])]);

        public static AlbumDetailsResponse HotFussAlbum() =>
            new(
                ArtistId.From("artist_the_killers"),
                "The Killers",
                AlbumId.From("album_hot_fuss"),
                "Hot Fuss",
                new DateOnly(2004, 6, 7),
                [MrBrightsideTrackSummary()]);

        public static TrackSummary MrBrightsideTrackSummary() =>
            new(
                TrackId.From("track_mr_brightside"),
                "Mr. Brightside",
                AlbumId.From("album_hot_fuss"),
                "Hot Fuss",
                "USIR20400274",
                222000,
                PlayabilityStatus.Playable,
                [ProviderName.Spotify, ProviderName.AppleMusic],
                [],
                [new ProviderReference(
                    ProviderName.Spotify,
                    "track",
                    "spotify-track-1",
                    new Uri("https://open.spotify.com/track/spotify-track-1"),
                    new DateTimeOffset(2026, 6, 17, 12, 0, 0, TimeSpan.Zero))]);

        public static TrackDetailsResponse MrBrightsideTrackDetails() =>
            new(
                ArtistId.From("artist_the_killers"),
                "The Killers",
                AlbumId.From("album_hot_fuss"),
                "Hot Fuss",
                TrackId.From("track_mr_brightside"),
                "Mr. Brightside",
                "USIR20400274",
                222000,
                PlayabilityStatus.Playable,
                [ProviderName.Spotify, ProviderName.AppleMusic],
                [],
                [new ProviderReference(
                    ProviderName.Spotify,
                    "track",
                    "spotify-track-1",
                    new Uri("https://open.spotify.com/track/spotify-track-1"),
                    new DateTimeOffset(2026, 6, 17, 12, 0, 0, TimeSpan.Zero))]);
    }
}
