using Soundtrail.Services.Features.Search.TrackSearch;
using Soundtrail.Services.Features.Tracks;

namespace Soundtrail.Services.Tests.Api.Unit.Infrastructure
{
    internal static class KnownSearchResults
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

        public static SearchResult WhenYouWereYoung() =>
            new(
                TrackTitle.From("When You Were Young"),
                ArtistName.From("The Killers"),
                Isrc.From("USIR20600065"),
                Mbid.From("when-you-were-young-mbid"),
                AppleId.From("apple-when-you-were-young"),
                SpotifyId.From("spotify-when-you-were-young"),
                ConfidenceScore.From(0.95));
    }
}