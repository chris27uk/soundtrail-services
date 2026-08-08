using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Contract;
using Soundtrail.Services.Api.Shared.Adapters;
using Soundtrail.Services.Api.Shared.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetArtist.Registrations;

public sealed class GetArtistResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<GetArtistResponse, GetArtistResponseDto>(
            toDto: response =>
                new GetArtistResponseDto(
                    response.ArtistId.Value,
                    response.ArtistName.Value,
                    response.Description,
                    response.ImageUrl,
                    ToDiscoveryDto(response.Discovery)),
            toDomainObject: dto =>
                new GetArtistResponse(
                    ArtistId.From(dto.ArtistId),
                    ArtistName.From(dto.ArtistName),
                    dto.Description,
                    dto.ImageUrl,
                    ToDiscovery(dto.Discovery)));

        registry.Register<CatalogArtistRecordDto, GetArtistResponse>(
            record =>
                new GetArtistResponse(
                    ArtistId.From(record.ArtistId),
                    ArtistName.From(record.Name),
                    Description: null,
                    record.ArtworkUrl,
                    Discovery: null));
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
