using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Api.Registrations;

public sealed class ArtistTracksResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<ArtistTracksResponse, ArtistTracksResponseDto>(
            translate: response =>
                new ArtistTracksResponseDto(
                    response.ArtistId.Value,
                    response.ArtistName,
                    response.Tracks.Select(x => registry.ToDto<TrackSummaryResponseDto>(x)).ToArray()));
    }
}
