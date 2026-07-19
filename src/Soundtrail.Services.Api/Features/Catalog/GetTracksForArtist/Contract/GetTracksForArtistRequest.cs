using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;

public sealed record GetTracksForArtistRequest(ArtistId ArtistId);
