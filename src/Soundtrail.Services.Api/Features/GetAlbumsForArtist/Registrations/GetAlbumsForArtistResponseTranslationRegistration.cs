using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Api.Features.GetAlbumsForArtist.Registrations;

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
                        .ToArray()),
            toDomainObject: dto =>
                new GetAlbumsForArtistResponse(
                    ArtistId.From(dto.ArtistId),
                    ArtistName.From(dto.ArtistName),
                    dto.Albums.Select(
                            album => new GetAlbumsForArtistAlbumResponse(
                                AlbumId.From(dto.ArtistId, album.AlbumId),
                                new MusicCatalogId.Album(AlbumId.From(dto.ArtistId, album.AlbumId)),
                                album.AlbumTitle,
                                album.ReleaseDate,
                                album.ArtworkUrl))
                        .ToArray()));

        registry.Register<CatalogArtistAlbumsRecordDto, GetAlbumsForArtistResponse>(
            translate: record =>
                new GetAlbumsForArtistResponse(
                    ArtistId.From(record.ArtistId),
                    ArtistName.From(record.ArtistName),
                    record.Albums.Select(
                            album => new GetAlbumsForArtistAlbumResponse(
                                AlbumId.From(record.ArtistId, album.AlbumId),
                                new MusicCatalogId.Album(AlbumId.From(record.ArtistId, album.AlbumId)),
                                album.AlbumTitle,
                                album.ReleaseDate,
                                album.ArtworkUrl))
                        .ToArray()));
    }
}
