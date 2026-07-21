using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;

public sealed record GetAlbumsForArtistRequest(ArtistId ArtistId);
