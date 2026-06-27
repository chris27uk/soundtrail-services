using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Search;
using Soundtrail.Translators.Registry;

namespace Soundtrail.Translators.Api.Registrations;

public sealed class SearchDiscoveryTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<SearchDiscovery, SearchDiscoveryResponseDto>(
            translate: discovery =>
                new SearchDiscoveryResponseDto(
                    discovery.WillBeLookedUp,
                    discovery.Reason,
                    discovery.RetryAfterSeconds));
    }
}
