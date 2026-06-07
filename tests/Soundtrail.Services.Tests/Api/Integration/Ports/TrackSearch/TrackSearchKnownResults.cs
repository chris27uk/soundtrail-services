using Soundtrail.Services.Api.Features.Search.Tracks;
using Soundtrail.Services.Api.Features.Search.TrackSearch;

namespace Soundtrail.Services.Tests.Api.Integration.Ports.TrackSearch
{
    internal static class TrackSearchKnownResults
    {
        public static SearchResult MrBrightside() =>
            new(
                TrackTitle.From("Mr. Brightside"),
                ArtistName.From("The Killers"),
                Isrc.From("USIR20400274"),
                Mbid.From("mr-brightside-mbid"),
                AppleId.From("apple-mr-brightside"),
                SpotifyId.From("spotify-mr-brightside"),
                ConfidenceScore.From(0.98));

        public static SearchResult MrBrightsideFromIndex() =>
            new(
                TrackTitle.From("Mr. Brightside"),
                ArtistName.From("The Killers"),
                Isrc.From("USIR20400274"),
                Mbid.From("mr-brightside-mbid"),
                AppleId.From("apple-mr-brightside"),
                SpotifyId.From("spotify-mr-brightside"),
                ConfidenceScore.From(0.95));
    }
}