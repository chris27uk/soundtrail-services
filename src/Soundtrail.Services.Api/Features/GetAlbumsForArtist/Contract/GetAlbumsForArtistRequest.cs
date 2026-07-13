using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Api.Features.GetAlbumsForArtist.Contract;

public sealed record GetAlbumsForArtistRequest(ArtistId ArtistId);
