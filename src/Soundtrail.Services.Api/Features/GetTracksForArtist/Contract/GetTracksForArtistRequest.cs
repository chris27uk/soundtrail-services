using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Api.Features.GetTracksForArtist.Contract;

public sealed record GetTracksForArtistRequest(ArtistId ArtistId);
