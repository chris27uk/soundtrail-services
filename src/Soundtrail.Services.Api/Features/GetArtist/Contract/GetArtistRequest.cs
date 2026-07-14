using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Services.Api.Features.GetArtist.Contract;

public sealed record GetArtistRequest(ArtistId ArtistId);
