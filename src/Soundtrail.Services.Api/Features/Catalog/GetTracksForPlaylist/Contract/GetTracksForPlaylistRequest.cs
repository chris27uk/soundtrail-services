using Soundtrail.Domain.Catalog.Playlists;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

public sealed record GetTracksForPlaylistRequest(PlaylistId PlaylistId);
