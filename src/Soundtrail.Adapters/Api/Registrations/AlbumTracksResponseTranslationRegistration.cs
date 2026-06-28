using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Api.Registrations;

public sealed class AlbumTracksResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<AlbumTracksResponse, AlbumTracksResponseDto>(
            translate: response =>
                new AlbumTracksResponseDto(
                    response.ArtistId.Value,
                    response.ArtistName,
                    response.AlbumId.Value,
                    response.AlbumName,
                    response.Tracks.Select(x => registry.ToDto<TrackSummaryResponseDto>(x)).ToArray()));
    }
}
