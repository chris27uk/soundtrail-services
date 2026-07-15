using Soundtrail.Adapters.Registry;
using Soundtrail.Services.Api.Features.GetAlbum.Contract;

namespace Soundtrail.Services.Api.Features.GetAlbum.Registrations;

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