using Soundtrail.Domain.Catalog.Albums;

namespace Soundtrail.Services.Api.Features.GetTracksForAlbum.Contract;

public sealed record GetTracksForAlbumRequest(AlbumId AlbumId);
