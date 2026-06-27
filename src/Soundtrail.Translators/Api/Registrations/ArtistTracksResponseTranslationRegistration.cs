using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Translators.Registry;

namespace Soundtrail.Translators.Api.Registrations;

public sealed class ArtistTracksResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<ArtistTracksResponse, ArtistTracksResponseDto>(
            translate: response =>
                new ArtistTracksResponseDto(
                    response.ArtistId.Value,
                    response.ArtistName,
                    response.Tracks.Select(x => registry.Translate<TrackSummaryResponseDto>(x)).ToArray()));
    }
}
