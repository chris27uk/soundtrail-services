using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Api.Features.GetArtist.Contract;

public sealed record GetArtistResponse(
    ArtistId ArtistId,
    ArtistName ArtistName,
    string? Description,
    string? ImageUrl);
