using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Translators.Registry;

namespace Soundtrail.Translators.Api.Registrations;

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
                    response.Tracks.Select(x => registry.Translate<TrackSummaryResponseDto>(x)).ToArray()));
    }
}
