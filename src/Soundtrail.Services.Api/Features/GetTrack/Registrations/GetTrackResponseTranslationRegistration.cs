using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.GetTrack.Adapters;
using Soundtrail.Services.Api.Features.GetTrack.Contract;

namespace Soundtrail.Services.Api.Features.GetTrack.Registrations;

public sealed class GetTrackResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<GetTrackResponse, GetTrackResponseDto>(
            toDto: response =>
                new GetTrackResponseDto(
                    response.TrackId.Value,
                    response.MusicCatalogId.NormalisedIdentifier,
                    response.Title,
                    response.ArtistName,
                    response.AlbumTitle,
                    response.DurationMs,
                    response.Isrc,
                    response.ReleaseDate,
                    response.ArtworkUrl),
            toDomainObject: dto =>
                new GetTrackResponse(
                    TrackId.From(dto.TrackId),
                    new CatalogItemId.Track(TrackId.From(dto.TrackId)),
                    dto.Title,
                    dto.ArtistName,
                    dto.AlbumTitle,
                    dto.DurationMs,
                    dto.Isrc,
                    dto.ReleaseDate,
                    dto.ArtworkUrl));

        registry.Register<CatalogTrackRecordDto, GetTrackResponse>(
            record =>
                new GetTrackResponse(
                    TrackId.FromKeyParts(
                        record.TrackIdBaseKeyHigh ?? throw new InvalidOperationException("Track base key high is required."),
                        record.TrackIdBaseKeyLow ?? throw new InvalidOperationException("Track base key low is required."),
                        record.TrackIdSpecificKey ?? throw new InvalidOperationException("Track specific key is required.")),
                    new CatalogItemId.Track(
                        TrackId.FromKeyParts(
                            record.TrackIdBaseKeyHigh ?? throw new InvalidOperationException("Track base key high is required."),
                            record.TrackIdBaseKeyLow ?? throw new InvalidOperationException("Track base key low is required."),
                            record.TrackIdSpecificKey ?? throw new InvalidOperationException("Track specific key is required."))),
                    record.Title,
                    record.ArtistName,
                    record.AlbumTitle,
                    record.DurationMs,
                    record.Isrc,
                    record.ReleaseDate,
                    record.ArtworkUrl));
    }
}
