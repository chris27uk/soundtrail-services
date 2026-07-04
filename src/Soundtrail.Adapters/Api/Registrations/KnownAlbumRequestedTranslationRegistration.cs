using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Api.Registrations;

public sealed class KnownAlbumRequestedTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<KnownAlbumRequested, KnownAlbumRequestedDto>(
            request =>
                new KnownAlbumRequestedDto(
                    request.ArtistId.Value,
                    request.AlbumId.Value,
                    request.OccurredAt,
                    request.CorrelationId.Value),
            dto =>
                new KnownAlbumRequested(
                    ArtistId.From(dto.ArtistId),
                    AlbumId.From(dto.AlbumId),
                    dto.OccurredAt,
                    CorrelationId.From(dto.CorrelationId)));
    }
}
