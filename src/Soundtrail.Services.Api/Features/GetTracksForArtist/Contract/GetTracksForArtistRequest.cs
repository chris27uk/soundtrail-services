using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Services.Api.Features.GetTracksForArtist.Contract;

public sealed record GetTracksForArtistRequest(ArtistId ArtistId);
