using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Api.Features.GetTracksForAlbum.Contract;

public sealed record GetTracksForAlbumRequest(AlbumId AlbumId);
