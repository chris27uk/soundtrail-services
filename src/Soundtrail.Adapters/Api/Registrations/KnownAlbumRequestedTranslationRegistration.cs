using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Api.Registrations;

public sealed class KnownAlbumRequestedTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<KnownAlbumRequested, KnownAlbumRequestedDto>(
            translate: request =>
                new KnownAlbumRequestedDto(
                    request.AlbumId.Value,
                    request.OccurredAt,
                    request.CorrelationId.Value));

        registry.Register<KnownAlbumRequestedDto, KnownAlbumRequested>(
            translate: dto =>
                new KnownAlbumRequested(
                    AlbumId.From(dto.AlbumId),
                    dto.OccurredAt,
                    CorrelationId.From(dto.CorrelationId)));
    }
}
