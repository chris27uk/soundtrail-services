using Soundtrail.Domain.Catalog.Playlists;

namespace Soundtrail.Services.Api.Features.GetTracksForPlaylist.Contract;

public sealed record GetTracksForPlaylistRequest(PlaylistId PlaylistId);
