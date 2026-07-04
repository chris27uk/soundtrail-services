using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Api.Registrations;

public sealed class KnownArtistRequestedTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<KnownArtistRequested, KnownArtistRequestedDto>(
            request =>
                new KnownArtistRequestedDto(
                    request.ArtistId.Value,
                    request.OccurredAt,
                    request.CorrelationId.Value),
            dto =>
                new KnownArtistRequested(
                    ArtistId.From(dto.ArtistId),
                    dto.OccurredAt,
                    CorrelationId.From(dto.CorrelationId)));
    }
}
