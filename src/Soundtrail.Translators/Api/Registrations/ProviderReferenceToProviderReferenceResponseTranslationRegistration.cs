using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Catalog;
using Soundtrail.Translators.Registry;

namespace Soundtrail.Translators.Api.Registrations;

public sealed class ProviderReferenceToProviderReferenceResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<ProviderReference, ProviderReferenceResponseDto>(
            translate: reference =>
                new ProviderReferenceResponseDto(
                    reference.Provider.ToPersistentId(),
                    reference.ProviderEntityType,
                    reference.ProviderId,
                    reference.Url,
                    reference.DiscoveredAt));
    }
}
