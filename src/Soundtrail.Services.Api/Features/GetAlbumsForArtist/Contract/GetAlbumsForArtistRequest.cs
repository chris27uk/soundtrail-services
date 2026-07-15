using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Services.Api.Features.GetAlbumsForArtist.Contract;

public sealed record GetAlbumsForArtistRequest(ArtistId ArtistId);
