using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;
using Soundtrail.Services.Api.Features.Catalog.Shared.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Registrations;

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
                                track.Title,
                                track.ArtistName,
                                track.AlbumTitle,
                                track.DurationMs,
                                track.Isrc,
                                track.ReleaseDate,
                                track.ArtworkUrl,
                                track.Playable,
                                ToStreamingLocationDtos(track.StreamingLocations)))
                        .ToArray(),
                    ToDiscoveryDto(response.Discovery)),
            toDomainObject: dto =>
                new GetTracksForArtistResponse(
                    ArtistId.From(dto.ArtistId),
                    ArtistName.From(dto.ArtistName),
                    dto.Tracks.Select(
                            track => new GetTracksForArtistTrackResponse(
                                TrackId.From(track.TrackId),
                                track.Title,
                                track.ArtistName,
                                track.AlbumTitle,
                                track.DurationMs,
                                track.Isrc,
                                track.ReleaseDate,
                                track.ArtworkUrl,
                                track.Playable,
                                ToStreamingLocations(track.StreamingLocations)))
                        .ToArray(),
                    ToDiscovery(dto.Discovery)));

        registry.Register<CatalogArtistTracksRecordDto, GetTracksForArtistResponse>(
            translate: record =>
                new GetTracksForArtistResponse(
                    ArtistId.From(record.ArtistId),
                    ArtistName.From(record.ArtistName),
                    record.Tracks.Select(
                            track => new GetTracksForArtistTrackResponse(
                                TrackId.From(track.TrackId),
                                track.Title,
                                track.ArtistName,
                                track.AlbumTitle,
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
