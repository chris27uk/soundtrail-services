using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Api.Registrations;

public sealed class KnownArtistRequestedTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<KnownArtistRequested, KnownArtistRequestedDto>(
            translate: request =>
                new KnownArtistRequestedDto(
                    request.ArtistId.Value,
                    request.OccurredAt,
                    request.CorrelationId.Value));

        registry.Register<KnownArtistRequestedDto, KnownArtistRequested>(
            translate: dto =>
                new KnownArtistRequested(
                    ArtistId.From(dto.ArtistId),
                    dto.OccurredAt,
                    CorrelationId.From(dto.CorrelationId)));
    }
}
