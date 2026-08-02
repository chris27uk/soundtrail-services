using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Discovery;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

public sealed record GetTracksForPlaylistResponse(
    PlaylistId PlaylistId,
    GetTracksForPlaylistTrackResponse[] Tracks,
    DiscoveryFeedbackResponse? Discovery = null)
{
    public static GetTracksForPlaylistResponse CatchingUp(PlaylistId playlistId, DateTimeOffset requestedAt)
    {
        return new GetTracksForPlaylistResponse(
            playlistId,
            [],
            PlaylistTracksDiscoveryFeedback
                .MissingProjection()
                .EstimateAt(requestedAt));
    }
}

public sealed record GetTracksForPlaylistTrackResponse(
    TrackId TrackId,
    string Title,
    string ArtistName,
    string? AlbumTitle,
    int? DurationMs,
    string? Isrc,
    DateOnly? ReleaseDate,
    string? ArtworkUrl,
    bool Playable,
    StreamingLocationResponse[] StreamingLocations);
