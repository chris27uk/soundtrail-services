using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Search;

public static class CatalogSearchCompletenessEvaluator
{
    private static readonly ProviderName[] SupportedPlaybackProviders =
    [
        ProviderName.Spotify,
        ProviderName.AppleMusic,
        ProviderName.YoutubeMusic
    ];

    public static bool IsComplete(SearchCatalogResult result) =>
        result.Type switch
        {
            SearchResultType.Artist => HasCoreArtistMetadata(result) && HasSettledPlaybackAvailability(result),
            SearchResultType.Album => HasCoreAlbumMetadata(result) && HasSettledPlaybackAvailability(result),
            SearchResultType.Track => HasCoreTrackMetadata(result) && HasSettledTrackPlayback(result),
            _ => false
        };

    private static bool HasCoreArtistMetadata(SearchCatalogResult result) =>
        !string.IsNullOrWhiteSpace(result.Id)
        && !string.IsNullOrWhiteSpace(result.Name)
        && !string.IsNullOrWhiteSpace(result.ArtistId)
        && !string.IsNullOrWhiteSpace(result.ArtistName);

    private static bool HasCoreAlbumMetadata(SearchCatalogResult result) =>
        HasCoreArtistMetadata(result)
        && !string.IsNullOrWhiteSpace(result.AlbumId)
        && !string.IsNullOrWhiteSpace(result.AlbumName);

    private static bool HasCoreTrackMetadata(SearchCatalogResult result) =>
        HasCoreAlbumMetadata(result);

    private static bool HasSettledPlaybackAvailability(SearchCatalogResult result) =>
        result.AvailableProviders.Count > 0
        || AreAllPlaybackProvidersTerminal(result.TerminallyUnavailableProviders);

    private static bool HasSettledTrackPlayback(SearchCatalogResult result) =>
        result.ProviderReferences.Count > 0
        || AreAllPlaybackProvidersTerminal(result.TerminallyUnavailableProviders);

    private static bool AreAllPlaybackProvidersTerminal(IReadOnlyList<ProviderName> terminalProviders) =>
        SupportedPlaybackProviders.All(provider => terminalProviders.Contains(provider));
}
