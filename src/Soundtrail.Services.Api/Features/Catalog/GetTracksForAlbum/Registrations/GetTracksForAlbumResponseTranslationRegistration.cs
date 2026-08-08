using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;
using Soundtrail.Services.Api.Shared.Adapters;
using Soundtrail.Services.Api.Shared.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Registrations;

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
                                track.Title,
                                track.ArtistName,
                                track.DurationMs,
                                track.Isrc,
                                track.ReleaseDate,
                                track.ArtworkUrl,
                                track.Playable,
                                ToStreamingLocationDtos(track.StreamingLocations)))
                        .ToArray(),
                    ToDiscoveryDto(response.Discovery)),
            toDomainObject: dto =>
                new GetTracksForAlbumResponse(
                    ArtistId.From(dto.ArtistId),
                    AlbumId.From(dto.ArtistId, dto.AlbumId),
                    dto.AlbumTitle,
                    dto.Tracks.Select(
                            track => new GetTracksForAlbumTrackResponse(
                                TrackId.From(track.TrackId),
                                track.Title,
                                track.ArtistName,
                                track.DurationMs,
                                track.Isrc,
                                track.ReleaseDate,
                                track.ArtworkUrl,
                                track.Playable,
                                ToStreamingLocations(track.StreamingLocations)))
                        .ToArray(),
                    ToDiscovery(dto.Discovery)));

        registry.Register<CatalogAlbumTracksRecordDto, GetTracksForAlbumResponse>(
            translate: record =>
                new GetTracksForAlbumResponse(
                    ArtistId.From(record.ArtistId),
                    AlbumId.From(record.ArtistId, record.AlbumId),
                    record.AlbumTitle,
                    record.Tracks.Select(
                            track => new GetTracksForAlbumTrackResponse(
                                TrackId.From(track.TrackId),
                                track.Title,
                                track.ArtistName,
                            track.DurationMs,
                            track.Isrc,
                            track.ReleaseDate,
                            track.ArtworkUrl,
                            track.StreamingLocations.Length > 0,
                            ToStreamingLocations(track.StreamingLocations)))
                        .ToArray(),
                    null));
    }

    private static StreamingLocationResponseDto[] ToStreamingLocationDtos(
        IEnumerable<StreamingLocationResponse> streamingLocations) =>
        streamingLocations
            .Select(static location => new StreamingLocationResponseDto(location.Provider, location.ExternalId, location.Url))
            .ToArray();

    private static StreamingLocationResponse[] ToStreamingLocations(
        IEnumerable<StreamingLocationResponseDto> streamingLocations) =>
        streamingLocations
            .Select(static location => new StreamingLocationResponse(location.Provider, location.ExternalId, location.Url))
            .ToArray();

    private static StreamingLocationResponse[] ToStreamingLocations(
        IEnumerable<CatalogStreamingLocationRecordDto> streamingLocations) =>
        streamingLocations
            .Select(static location => new StreamingLocationResponse(location.Provider, location.ExternalId, location.Url))
            .ToArray();

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
