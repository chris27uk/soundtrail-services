using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Catalog;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Api.Registrations;

public sealed class ProviderReferenceToProviderReferenceResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<ProviderReference, ProviderReferenceResponseDto>(
            translate: reference =>
                new ProviderReferenceResponseDto(
                    reference.Provider.StableValue,
                    reference.ProviderEntityType,
                    reference.ProviderId,
                    reference.Url,
                    reference.DiscoveredAt));
    }
}
