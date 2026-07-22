using Soundtrail.Services.Api.Features.Catalog.Shared.Adapters;

namespace Soundtrail.Services.Api.Features.Catalog.GetArtist.Adapters;

public sealed record GetArtistResponseDto(
    string ArtistId,
    string ArtistName,
    string? Description,
    string? ImageUrl,
    DiscoveryFeedbackResponseDto? Discovery);
