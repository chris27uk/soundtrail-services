using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Adapters
{
    public class StreamingLocationsCollection
    {
        private IReadOnlyDictionary<string, OdesliStreamingReferences.OdesliPlatformLinkDto> linksByPlatform;
        
        public StreamingLocation? TryRetrieveYoutubeLink()
        {
            if (!linksByPlatform.TryGetValue("youtubeMusic", out var youtubeMusic)
                || string.IsNullOrWhiteSpace(youtubeMusic.Url))
            {
                return null;
            }

            var uri = new Uri(youtubeMusic.Url, UriKind.Absolute);
            var videoId = GetQueryParameter(uri, "v");

            if (string.IsNullOrWhiteSpace(videoId))
            {
                return null;
            }

            return new StreamingLocation(
                ProviderName.YoutubeMusic,
                uri,
                videoId,
                LookupSource.Odesli,
                
            );
        }

        public StreamingLocation? GetSpotifyLink()
        {
            if (!linksByPlatform.TryGetValue("spotify", out var spotify)
                || string.IsNullOrWhiteSpace(spotify.Url))
            {
                return null;
            }

            var uri = new Uri(spotify.Url, UriKind.Absolute);
            var trackSegment = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .SkipWhile(segment => !string.Equals(segment, "track", StringComparison.OrdinalIgnoreCase))
                .Skip(1)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(trackSegment))
            {
                return null;
            }

            return new StreamingLocation(
                ProviderName.Spotify,
                trackSegment,
                uri,
                LookupSource.Odesli,
                clockPort.UtcNow);
        }

        public StreamingLocation? AppleMusicLink()
        {
            if (!linksByPlatform.TryGetValue("appleMusic", out var appleMusic)
                || string.IsNullOrWhiteSpace(appleMusic.Url))
            {
                return null;
            }

            var uri = new Uri(appleMusic.Url, UriKind.Absolute);
            var trackId = GetQueryParameter(uri, "i");

            if (string.IsNullOrWhiteSpace(trackId))
            {
                return null;
            }

            return new StreamingLocation(
                ProviderName.AppleMusic,
                trackId,
                uri,
                LookupSource.Odesli,
                clockPort.UtcNow);
        }

        private static string? GetQueryParameter(Uri uri, string key)
        {
            var query = uri.Query.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries);

            foreach (var pair in query)
            {
                var parts = pair.Split('=', 2);

                if (parts.Length == 2 && string.Equals(parts[0], key, StringComparison.OrdinalIgnoreCase))
                {
                    return Uri.UnescapeDataString(parts[1]);
                }
            }

            return null;
        }
    }
}
