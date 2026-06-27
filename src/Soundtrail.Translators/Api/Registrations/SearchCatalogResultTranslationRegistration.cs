using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Search;
using Soundtrail.Translators.Registry;

namespace Soundtrail.Translators.Api.Registrations;

public sealed class SearchCatalogResultTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<SearchCatalogResult, SearchCatalogResultResponseDto>(
            translate: result =>
                new SearchCatalogResultResponseDto(
                    ToResponseType(result.Type),
                    result.Id,
                    result.Name,
                    result.ArtistId,
                    result.ArtistName,
                    result.AlbumId,
                    result.AlbumName,
                    result.PlayabilityStatus.ToString(),
                    result.AvailableProviders.Select(x => x.ToPersistentId()).ToArray(),
                    result.TerminallyUnavailableProviders.Select(x => x.ToPersistentId()).ToArray(),
                    result.ProviderReferences.Select(x => registry.Translate<ProviderReferenceResponseDto>(x)).ToArray()));
    }

    private static string ToResponseType(SearchResultType type) =>
        type switch
        {
            SearchResultType.Artist => "artist",
            SearchResultType.Album => "album",
            SearchResultType.Track => "track",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}
