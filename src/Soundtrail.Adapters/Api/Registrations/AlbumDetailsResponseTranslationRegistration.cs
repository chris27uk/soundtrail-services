using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Api.Registrations;

public sealed class AlbumDetailsResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<AlbumDetailsResponse, AlbumDetailsResponseDto>(
            translate: response =>
                new AlbumDetailsResponseDto(
                    response.ArtistId.Value,
                    response.ArtistName,
                    response.AlbumId.Value,
                    response.Name,
                    response.ReleaseDate,
                    response.Tracks.Select(x => registry.ToDto<TrackSummaryResponseDto>(x)).ToArray()));
    }
}
