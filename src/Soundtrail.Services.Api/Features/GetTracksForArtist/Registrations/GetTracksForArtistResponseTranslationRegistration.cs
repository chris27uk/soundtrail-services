using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetTracksForArtist.Contract;

namespace Soundtrail.Services.Api.Features.GetTracksForArtist.Registrations;

public sealed class GetTracksForArtistResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<GetTracksForArtistResponse, GetTracksForArtistResponseDto>(
            toDto: response =>
                new GetTracksForArtistResponseDto(
                    response.ArtistId.Value,
                    response.ArtistName.Value,
                    response.Tracks.Select(
                            track => new GetTracksForArtistTrackResponseDto(
                                track.TrackId.Value,
                                track.MusicCatalogId.NormalisedIdentifier,
                                track.Title,
                                track.ArtistName,
                                track.AlbumTitle,
                                track.DurationMs,
                                track.Isrc,
                                track.ReleaseDate,
                                track.ArtworkUrl))
                        .ToArray()),
            toDomainObject: dto =>
                new GetTracksForArtistResponse(
                    ArtistId.From(dto.ArtistId),
                    ArtistName.From(dto.ArtistName),
                    dto.Tracks.Select(
                            track => new GetTracksForArtistTrackResponse(
                                TrackId.From(track.TrackId),
                                new MusicCatalogId.Track(TrackId.From(track.TrackId)),
                                track.Title,
                                track.ArtistName,
                                track.AlbumTitle,
                                track.DurationMs,
                                track.Isrc,
                                track.ReleaseDate,
                                track.ArtworkUrl))
                        .ToArray()));

        registry.Register<CatalogArtistTracksRecordDto, GetTracksForArtistResponse>(
            translate: record =>
                new GetTracksForArtistResponse(
                    ArtistId.From(record.ArtistId),
                    ArtistName.From(record.ArtistName),
                    record.Tracks.Select(
                            track => new GetTracksForArtistTrackResponse(
                                TrackId.From(track.TrackId),
                                new MusicCatalogId.Track(TrackId.From(track.TrackId)),
                                track.Title,
                                track.ArtistName,
                                track.AlbumTitle,
                                track.DurationMs,
                                track.Isrc,
                                track.ReleaseDate,
                                track.ArtworkUrl))
                        .ToArray()));
    }
}
