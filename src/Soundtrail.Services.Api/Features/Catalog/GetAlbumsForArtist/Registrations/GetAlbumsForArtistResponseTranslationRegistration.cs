using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;
using Soundtrail.Services.Api.Features.Catalog.Shared.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Registrations;

public sealed class GetAlbumsForArtistResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<GetAlbumsForArtistResponse, GetAlbumsForArtistResponseDto>(
            toDto: response =>
                new GetAlbumsForArtistResponseDto(
                    response.ArtistId.Value,
                    response.ArtistName.Value,
                    response.Albums.Select(
                            album => new GetAlbumsForArtistAlbumResponseDto(
                                album.AlbumId.ArtistAlbumId,
                                album.MusicCatalogId.NormalisedIdentifier,
                                album.AlbumTitle,
                                album.ReleaseDate,
                                album.ArtworkUrl))
                        .ToArray(),
                    ToDiscoveryDto(response.Discovery)),
            toDomainObject: dto =>
                new GetAlbumsForArtistResponse(
                    ArtistId.From(dto.ArtistId),
                    ArtistName.From(dto.ArtistName),
                    dto.Albums.Select(
                            album => new GetAlbumsForArtistAlbumResponse(
                                AlbumId.From(dto.ArtistId, album.AlbumId),
                                new CatalogItemId.Album(AlbumId.From(dto.ArtistId, album.AlbumId)),
                                album.AlbumTitle,
                                album.ReleaseDate,
                                album.ArtworkUrl))
                        .ToArray(),
                    ToDiscovery(dto.Discovery)));

        registry.Register<CatalogArtistAlbumsRecordDto, GetAlbumsForArtistResponse>(
            translate: record =>
                new GetAlbumsForArtistResponse(
                    ArtistId.From(record.ArtistId),
                    ArtistName.From(record.ArtistName),
                    record.Albums.Select(
                            album => new GetAlbumsForArtistAlbumResponse(
                                AlbumId.From(record.ArtistId, album.AlbumId),
                                new CatalogItemId.Album(AlbumId.From(record.ArtistId, album.AlbumId)),
                            album.AlbumTitle,
                            album.ReleaseDate,
                            album.ArtworkUrl))
                        .ToArray(),
                    null));
    }

    private static DiscoveryFeedbackResponseDto? ToDiscoveryDto(DiscoveryFeedbackResponse? discovery) =>
        discovery is null
            ? null
            : new DiscoveryFeedbackResponseDto(
                discovery.Status,
                discovery.Priority.ToString(),
                discovery.NextEligibleAt,
                discovery.EarliestExpectedCompletionAt,
                discovery.Reason,
                discovery.UpdatedAtUtc);

    private static DiscoveryFeedbackResponse? ToDiscovery(DiscoveryFeedbackResponseDto? discovery) =>
        discovery is null
            ? null
            : new DiscoveryFeedbackResponse(
                discovery.Status,
                Enum.Parse<Soundtrail.Domain.Common.LookupPriorityBand>(discovery.Priority, true),
                discovery.NextEligibleAtUtc,
                discovery.EarliestExpectedCompletionAtUtc,
                discovery.Reason,
                discovery.UpdatedAtUtc);
}
