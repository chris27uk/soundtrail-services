using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetArtist.Contract;

public sealed record GetArtistResponse(
    ArtistId ArtistId,
    ArtistName ArtistName,
    string? Description,
    string? ImageUrl,
    DiscoveryFeedbackResponse? Discovery = null);
