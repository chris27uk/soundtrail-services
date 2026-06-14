using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Api.Features;

internal static class ProviderContract
{
    public static string ToValue(ProviderName provider) =>
        provider.Value switch
        {
            "Spotify" => "spotify",
            "AppleMusic" => "appleMusic",
            "YoutubeMusic" => "youtubeMusic",
            _ => provider.Value
        };
}
