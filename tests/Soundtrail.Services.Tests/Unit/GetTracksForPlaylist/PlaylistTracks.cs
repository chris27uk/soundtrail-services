using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Tests.Unit.GetTracksForPlaylist;

internal static class PlaylistTracks
{
    public static PlaylistId DefaultPlaylistId => PlaylistId.FromPlaylistName("WorldwideSongChart");

    public static TrackId DefaultTrackId => TestTrackIds.Create("track-3201");

    public static GetTracksForPlaylistResponse CreateResponse(
        PlaylistId? playlistId = null,
        TrackId? trackId = null,
        string title = "The Track",
        string artistName = "The Artist",
        string? albumTitle = "The Album",
        int? durationMs = 201000,
        string? isrc = "GBAYE2403201",
        DateOnly? releaseDate = null,
        string? artworkUrl = "https://cdn.soundtrail.test/tracks/track-3201.jpg")
    {
        var resolvedPlaylistId = playlistId ?? DefaultPlaylistId;
        var resolvedTrackId = trackId ?? DefaultTrackId;

        return new GetTracksForPlaylistResponse(
            resolvedPlaylistId,
            [
                new GetTracksForPlaylistTrackResponse(
                    resolvedTrackId,
                    new CatalogItemId.Track(resolvedTrackId),
                    title,
                    artistName,
                    albumTitle,
                    durationMs,
                    isrc,
                    releaseDate ?? new DateOnly(2024, 1, 2),
                    artworkUrl)
            ]);
    }
}
