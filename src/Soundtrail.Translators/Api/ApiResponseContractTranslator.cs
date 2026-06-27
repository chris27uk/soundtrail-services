using Soundtrail.Contracts.Api;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Domain.Search;

namespace Soundtrail.Translators.Api;

public static class ApiResponseContractTranslator
{
    public static SearchCatalogResponseDto ToDto(SearchCatalogResponse response) =>
        new(
            response.Query,
            response.Results.Select(ToDto).ToArray(),
            ToDto(response.Discovery));

    public static ArtistDetailsResponseDto ToDto(ArtistDetailsResponse response) =>
        new(
            response.ArtistId.Value,
            response.Name,
            response.Albums.Select(ToDto).ToArray());

    public static ArtistTracksResponseDto ToDto(ArtistTracksResponse response) =>
        new(
            response.ArtistId.Value,
            response.ArtistName,
            response.Tracks.Select(ToDto).ToArray());

    public static AlbumDetailsResponseDto ToDto(AlbumDetailsResponse response) =>
        new(
            response.ArtistId.Value,
            response.ArtistName,
            response.AlbumId.Value,
            response.Name,
            response.ReleaseDate,
            response.Tracks.Select(ToDto).ToArray());

    public static AlbumTracksResponseDto ToDto(AlbumTracksResponse response) =>
        new(
            response.ArtistId.Value,
            response.ArtistName,
            response.AlbumId.Value,
            response.AlbumName,
            response.Tracks.Select(ToDto).ToArray());

    public static TrackDetailsResponseDto ToDto(TrackDetailsResponse response) =>
        new(
            response.ArtistId.Value,
            response.ArtistName,
            response.AlbumId.Value,
            response.AlbumName,
            response.TrackId.Value,
            response.Title,
            response.Isrc,
            response.DurationMs,
            response.PlayabilityStatus.ToString(),
            response.AvailableProviders.Select(ToPersistentId).ToArray(),
            response.TerminallyUnavailableProviders.Select(ToPersistentId).ToArray(),
            response.ProviderReferences.Select(ToDto).ToArray());

    private static SearchCatalogResultResponseDto ToDto(SearchCatalogResult result) =>
        new(
            ToDto(result.Type),
            result.Id,
            result.Name,
            result.ArtistId,
            result.ArtistName,
            result.AlbumId,
            result.AlbumName,
            result.PlayabilityStatus.ToString(),
            result.AvailableProviders.Select(ToPersistentId).ToArray(),
            result.TerminallyUnavailableProviders.Select(ToPersistentId).ToArray(),
            result.ProviderReferences.Select(ToDto).ToArray());

    private static SearchDiscoveryResponseDto ToDto(SearchDiscovery discovery) =>
        new(
            discovery.WillBeLookedUp,
            discovery.Reason,
            discovery.RetryAfterSeconds);

    private static AlbumSummaryResponseDto ToDto(AlbumSummary album) =>
        new(
            album.AlbumId.Value,
            album.Name,
            album.ReleaseDate,
            album.PlayabilityStatus.ToString(),
            album.AvailableProviders.Select(ToPersistentId).ToArray(),
            album.TerminallyUnavailableProviders.Select(ToPersistentId).ToArray());

    private static TrackSummaryResponseDto ToDto(TrackSummary track) =>
        new(
            track.TrackId.Value,
            track.Title,
            track.AlbumId.Value,
            track.AlbumName,
            track.Isrc,
            track.DurationMs,
            track.PlayabilityStatus.ToString(),
            track.AvailableProviders.Select(ToPersistentId).ToArray(),
            track.TerminallyUnavailableProviders.Select(ToPersistentId).ToArray(),
            track.ProviderReferences.Select(ToDto).ToArray());

    private static ProviderReferenceResponseDto ToDto(ProviderReference reference) =>
        new(
            ToPersistentId(reference.Provider),
            reference.ProviderEntityType,
            reference.ProviderId,
            reference.Url,
            reference.DiscoveredAt);

    private static string ToPersistentId(ProviderName provider) => provider.ToPersistentId();

    private static string ToDto(SearchResultType type) =>
        type switch
        {
            SearchResultType.Artist => "artist",
            SearchResultType.Album => "album",
            SearchResultType.Track => "track",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}
