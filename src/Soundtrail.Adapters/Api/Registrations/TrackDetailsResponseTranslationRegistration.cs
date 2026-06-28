using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Api.Registrations;

public sealed class TrackDetailsResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<TrackDetailsResponse, TrackDetailsResponseDto>(
            translate: response =>
                new TrackDetailsResponseDto(
                    response.ArtistId.Value,
                    response.ArtistName,
                    response.AlbumId.Value,
                    response.AlbumName,
                    response.TrackId.Value,
                    response.Title,
                    response.Isrc,
                    response.DurationMs,
                    response.PlayabilityStatus.ToString(),
                    response.AvailableProviders.Select(x => x.StableValue).ToArray(),
                    response.TerminallyUnavailableProviders.Select(x => x.StableValue).ToArray(),
                    response.ProviderReferences.Select(x => registry.ToDto<ProviderReferenceResponseDto>(x)).ToArray()));
    }
}
