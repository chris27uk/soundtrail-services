using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Contract;
using Soundtrail.Services.Api.Features.Catalog.Shared.Adapters;

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
                    response.ReleaseDate,
                    response.Discovery is null
                        ? null
                        : new DiscoveryFeedbackResponseDto(
                            response.Discovery.Status,
                            response.Discovery.Priority.ToString(),
                            response.Discovery.NextEligibleAt,
                            response.Discovery.EarliestExpectedCompletionAt,
                            response.Discovery.Reason,
                            response.Discovery.UpdatedAtUtc)));
    }
}
