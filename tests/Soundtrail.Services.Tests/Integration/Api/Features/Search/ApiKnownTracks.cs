using Soundtrail.Services.Api.Features.Search.Tracks;
using Soundtrail.Services.Api.Features.Search.TrackSearch;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
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
                []);

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
                []);

        public static SearchResult MrBrightside() =>
            new(
                TrackTitle.From("Mr. Brightside"),
                ArtistName.From("The Killers"),
                Isrc.From("USIR20400274"),
                Mbid.From("mr-brightside-mbid"),
                AppleId.From("apple-mr-brightside"),
                SpotifyId.From("spotify-mr-brightside"),
                ConfidenceScore.From(0.98));

        public static SearchResult Track(string title) =>
            new(
                TrackTitle.From(title),
                ArtistName.From("Search Fixture Artist"),
                Isrc.From($"USFIX{title.Replace(" ", string.Empty).PadLeft(7, '0')}"),
                Mbid.From($"mbid-{title.Replace(" ", "-")}"),
                AppleId.From($"apple-{title.Replace(" ", "-")}"),
                SpotifyId.From($"spotify-{title.Replace(" ", "-")}"),
                ConfidenceScore.From(0.75));

        public static SearchResult SongWithConfidence(string title, double confidence) =>
            new(
                TrackTitle.From(title),
                ArtistName.From("Confidence Fixture Artist"),
                Isrc.From($"USCF{title.Replace(" ", string.Empty).PadLeft(8, '0')}"),
                Mbid.From($"mbid-{title.Replace(" ", "-")}"),
                AppleId.From($"apple-{title.Replace(" ", "-")}"),
                SpotifyId.From($"spotify-{title.Replace(" ", "-")}"),
                ConfidenceScore.From(confidence));

        public static Track MrBrightsideTrack() =>
            new(
                TrackTitle.From("Mr. Brightside"),
                ArtistName.From("The Killers"),
                Isrc.From("USIR20400274"),
                Mbid.From("mr-brightside-mbid"),
                AppleId.From("apple-mr-brightside"),
                SpotifyId.From("spotify-mr-brightside"),
                DurationMs.From(222000));
    }
}
