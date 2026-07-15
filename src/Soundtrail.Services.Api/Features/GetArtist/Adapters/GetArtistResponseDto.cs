namespace Soundtrail.Services.Api.Features.GetArtist.Registrations;

public sealed record GetArtistResponseDto(
    string ArtistId,
    string ArtistName,
    string? Description,
    string? ImageUrl);
