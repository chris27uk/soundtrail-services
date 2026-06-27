using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Translators.Registry;

namespace Soundtrail.Translators.Api.Registrations;

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
                    response.AvailableProviders.Select(x => x.ToPersistentId()).ToArray(),
                    response.TerminallyUnavailableProviders.Select(x => x.ToPersistentId()).ToArray(),
                    response.ProviderReferences.Select(x => registry.Translate<ProviderReferenceResponseDto>(x)).ToArray()));
    }
}
