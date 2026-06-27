using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Search;
using Soundtrail.Translators.Registry;

namespace Soundtrail.Translators.Api.Registrations;

public sealed class SearchCatalogResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<SearchCatalogResponse, SearchCatalogResponseDto>(
            translate: response =>
                new SearchCatalogResponseDto(
                    response.Query,
                    response.Results.Select(x => registry.Translate<SearchCatalogResultResponseDto>(x)).ToArray(),
                    registry.Translate<SearchDiscoveryResponseDto>(response.Discovery)));
    }
}
