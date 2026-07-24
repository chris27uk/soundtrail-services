using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Contract;
using Soundtrail.Services.Api.Features.Catalog.Shared.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTrack.Registrations;

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
                    response.ArtworkUrl,
                    ToDiscoveryDto(response.Discovery)),
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
                    dto.ArtworkUrl,
                    ToDiscovery(dto.Discovery)));

        registry.Register<CatalogTrackRecordDto, GetTrackResponse>(
            record =>
                new GetTrackResponse(
                    TrackId.From(record.TrackId),
                    new CatalogItemId.Track(TrackId.From(record.TrackId)),
                    record.Title,
                    record.ArtistName,
                    record.AlbumTitle,
                    record.DurationMs,
                    record.Isrc,
                    record.ReleaseDate,
                    record.ArtworkUrl,
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
