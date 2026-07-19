using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Services.Api.Features.Catalog.GetArtist.Contract;

public sealed record GetArtistRequest(ArtistId ArtistId);
