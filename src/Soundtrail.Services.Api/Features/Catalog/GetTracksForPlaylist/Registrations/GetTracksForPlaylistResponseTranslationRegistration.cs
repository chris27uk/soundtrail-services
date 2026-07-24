using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Api.Features.Catalog.Shared.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Registrations;

public sealed class GetTracksForPlaylistResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<GetTracksForPlaylistResponse, GetTracksForPlaylistResponseDto>(
            toDto: response =>
                new GetTracksForPlaylistResponseDto(
                    response.PlaylistId.Value,
                    response.Tracks.Select(
                            track => new GetTracksForPlaylistTrackResponseDto(
                                track.TrackId.Value,
                                track.MusicCatalogId.NormalisedIdentifier,
                                track.Title,
                                track.ArtistName,
                                track.AlbumTitle,
                                track.DurationMs,
                                track.Isrc,
                                track.ReleaseDate,
                                track.ArtworkUrl))
                        .ToArray(),
                    ToDiscoveryDto(response.Discovery)),
            toDomainObject: dto =>
                new GetTracksForPlaylistResponse(
                    PlaylistId.FromPlaylistName(dto.PlaylistId),
                    dto.Tracks.Select(
                            track => new GetTracksForPlaylistTrackResponse(
                                TrackId.From(track.TrackId),
                                new CatalogItemId.Track(TrackId.From(track.TrackId)),
                                track.Title,
                                track.ArtistName,
                                track.AlbumTitle,
                                track.DurationMs,
                                track.Isrc,
                                track.ReleaseDate,
                                track.ArtworkUrl))
                        .ToArray(),
                    ToDiscovery(dto.Discovery)));

        registry.Register<CatalogPlaylistTracksRecordDto, GetTracksForPlaylistResponse>(
            record =>
                new GetTracksForPlaylistResponse(
                    PlaylistId.FromPlaylistName(record.PlaylistId),
                    record.Tracks.Select(
                            track => new GetTracksForPlaylistTrackResponse(
                                TrackId.From(track.TrackId),
                                new CatalogItemId.Track(TrackId.From(track.TrackId)),
                                track.Title,
                                track.ArtistName,
                                track.AlbumTitle,
                            track.DurationMs,
                            track.Isrc,
                            track.ReleaseDate,
                            track.ArtworkUrl))
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
