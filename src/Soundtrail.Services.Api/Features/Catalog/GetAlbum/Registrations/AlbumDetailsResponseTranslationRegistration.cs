using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetAlbum.Registrations;

public sealed class AlbumDetailsResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<GetAlbumResponse, GetAlbumResponseDto>(
            translate: response =>
                new GetAlbumResponseDto(
                    response.ArtistId.Value,
                    response.ArtistName.Value,
                    response.AlbumId.ArtistAlbumId,
                    response.ReleaseDate));
    }
}