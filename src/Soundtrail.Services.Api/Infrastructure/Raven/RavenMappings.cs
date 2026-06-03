using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Features.Tracks;

namespace Soundtrail.Services.Api.Infrastructure.Raven;

internal static class RavenMappings
{
    public static SearchResult ToDomain(this RavenSearchResultDocument document) =>
        new(
            TrackTitle.From(document.Title),
            ArtistName.From(document.Artist),
            string.IsNullOrWhiteSpace(document.Isrc) ? null : Isrc.From(document.Isrc),
            string.IsNullOrWhiteSpace(document.Mbid) ? null : Mbid.From(document.Mbid),
            string.IsNullOrWhiteSpace(document.AppleId) ? null : AppleId.From(document.AppleId),
            string.IsNullOrWhiteSpace(document.SpotifyId) ? null : SpotifyId.From(document.SpotifyId),
            ConfidenceScore.From(document.Confidence));

    public static RavenSearchResultDocument ToDocument(this SearchResult result) =>
        new()
        {
            Title = result.Title.Value,
            Artist = result.Artist.Value,
            Isrc = result.Isrc?.Value,
            Mbid = result.Mbid?.Value,
            AppleId = result.AppleId?.Value,
            SpotifyId = result.SpotifyId?.Value,
            Confidence = result.Confidence.Value
        };

    public static Track ToDomainTrack(this RavenTrackDocument document) =>
        new(
            TrackTitle.From(document.Title),
            ArtistName.From(document.Artist),
            string.IsNullOrWhiteSpace(document.Isrc) ? null : Isrc.From(document.Isrc),
            string.IsNullOrWhiteSpace(document.Mbid) ? null : Mbid.From(document.Mbid),
            string.IsNullOrWhiteSpace(document.AppleId) ? null : AppleId.From(document.AppleId),
            string.IsNullOrWhiteSpace(document.SpotifyId) ? null : SpotifyId.From(document.SpotifyId),
            document.DurationMs is null ? null : DurationMs.From(document.DurationMs.Value));

    public static RavenTrackDocument ToDocument(this Track track, string stableId) =>
        new()
        {
            Id = RavenTrackDocument.GetDocumentId(stableId),
            Title = track.Title.Value,
            Artist = track.Artist.Value,
            SearchText = RavenTrackDocument.BuildSearchText(track.Title.Value, track.Artist.Value),
            Isrc = track.Isrc?.Value,
            Mbid = track.Mbid?.Value,
            AppleId = track.AppleId?.Value,
            SpotifyId = track.SpotifyId?.Value,
            DurationMs = track.Duration?.Value
        };
}
