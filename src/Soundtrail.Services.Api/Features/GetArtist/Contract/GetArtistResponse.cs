using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Services.Api.Features.GetArtist.Contract;

public sealed record GetArtistResponse(
    ArtistId ArtistId,
    ArtistName ArtistName,
    string? Description,
    string? ImageUrl);
