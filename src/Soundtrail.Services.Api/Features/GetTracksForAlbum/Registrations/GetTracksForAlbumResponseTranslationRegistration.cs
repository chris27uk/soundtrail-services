using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.GetTracksForAlbum.Adapters;
using Soundtrail.Services.Api.Features.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Api.Features.GetTracksForAlbum.Registrations;

public sealed class GetTracksForAlbumResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<GetTracksForAlbumResponse, GetTracksForAlbumResponseDto>(
            toDto: response =>
                new GetTracksForAlbumResponseDto(
                    response.ArtistId.Value,
                    response.AlbumId.ArtistAlbumId,
                    response.AlbumTitle,
                    response.Tracks.Select(
                            track => new GetTracksForAlbumTrackResponseDto(
                                track.TrackId.Value,
                                track.MusicCatalogId.NormalisedIdentifier,
                                track.Title,
                                track.ArtistName,
                                track.DurationMs,
                                track.Isrc,
                                track.ReleaseDate,
                                track.ArtworkUrl))
                        .ToArray()),
            toDomainObject: dto =>
                new GetTracksForAlbumResponse(
                    ArtistId.From(dto.ArtistId),
                    AlbumId.From(dto.ArtistId, dto.AlbumId),
                    dto.AlbumTitle,
                    dto.Tracks.Select(
                            track => new GetTracksForAlbumTrackResponse(
                                TrackId.From(track.TrackId),
                                new CatalogItemId.Track(TrackId.From(track.TrackId)),
                                track.Title,
                                track.ArtistName,
                                track.DurationMs,
                                track.Isrc,
                                track.ReleaseDate,
                                track.ArtworkUrl))
                        .ToArray()));

        registry.Register<CatalogAlbumTracksRecordDto, GetTracksForAlbumResponse>(
            translate: record =>
                new GetTracksForAlbumResponse(
                    ArtistId.From(record.ArtistId),
                    AlbumId.From(record.ArtistId, record.AlbumId),
                    record.AlbumTitle,
                    record.Tracks.Select(
                            track => new GetTracksForAlbumTrackResponse(
                                TrackId.FromKeyParts(
                                    track.TrackIdBaseKeyHigh ?? throw new InvalidOperationException("Track base key high is required."),
                                    track.TrackIdBaseKeyLow ?? throw new InvalidOperationException("Track base key low is required."),
                                    track.TrackIdSpecificKey ?? throw new InvalidOperationException("Track specific key is required.")),
                                new CatalogItemId.Track(
                                    TrackId.FromKeyParts(
                                        track.TrackIdBaseKeyHigh ?? throw new InvalidOperationException("Track base key high is required."),
                                        track.TrackIdBaseKeyLow ?? throw new InvalidOperationException("Track base key low is required."),
                                        track.TrackIdSpecificKey ?? throw new InvalidOperationException("Track specific key is required."))),
                                track.Title,
                                track.ArtistName,
                                track.DurationMs,
                                track.Isrc,
                                track.ReleaseDate,
                                track.ArtworkUrl))
                        .ToArray()));
    }
}
