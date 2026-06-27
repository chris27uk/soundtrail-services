using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Search;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Api.Registrations;

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
