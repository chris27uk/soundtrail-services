using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Search;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Api.Registrations;

public sealed class SearchCatalogResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<SearchCatalogResponse, SearchCatalogResponseDto>(
            translate: response =>
                new SearchCatalogResponseDto(
                    response.Query,
                    response.Results.Select(x => registry.ToDto<SearchCatalogResultResponseDto>(x)).ToArray(),
                    registry.ToDto<SearchDiscoveryResponseDto>(response.Discovery)));
    }
}
