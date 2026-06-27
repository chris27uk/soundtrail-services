using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Api.Registrations;

public sealed class ArtistDetailsResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<ArtistDetailsResponse, ArtistDetailsResponseDto>(
            translate: response =>
                new ArtistDetailsResponseDto(
                    response.ArtistId.Value,
                    response.Name,
                    response.Albums.Select(x => registry.Translate<AlbumSummaryResponseDto>(x)).ToArray()));
    }
}
