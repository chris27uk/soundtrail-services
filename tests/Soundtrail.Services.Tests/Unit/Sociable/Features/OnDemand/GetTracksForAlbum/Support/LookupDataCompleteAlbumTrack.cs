using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum.Support;

internal sealed record LookupDataCompleteAlbumTrack(
    CatalogDiscoveryEntry CatalogEntry,
    IReadOnlyDictionary<ProviderName, Uri> StreamingLocations)
{
    public static LookupDataCompleteAlbumTrack Create(
        AlbumId albumId,
        string artistName,
        string title,
        string albumTitle,
        DateOnly releaseDate,
        string? releaseType,
        int durationMs,
        DateTimeOffset catalogUpdatedAt,
        string? isrc = null,
        string? artworkUrl = null,
        params (ProviderName Provider, string Url)[] streamingLocations)
    {
        var trackId = TrackId.TryCreate(artistName, title, albumTitle, releaseDate, releaseType) switch
        {
            TrackIdCreateResult.Success success => success.Value,
            TrackIdCreateResult.Failure failure => throw new InvalidOperationException(failure.Reason),
            _ => throw new InvalidOperationException("Unsupported track id creation result.")
        };

        var track = new Track(trackId)
        {
            Title = title,
            ArtistName = artistName,
            AlbumTitle = albumTitle,
            AlbumId = albumId.StableValue,
            DurationMs = durationMs,
            Isrc = isrc,
            ReleaseDate = releaseDate,
            ReleaseType = releaseType,
            ArtworkUrl = artworkUrl,
            UpdatedAt = catalogUpdatedAt
        };

        return new LookupDataCompleteAlbumTrack(
            new CatalogDiscoveryEntry(ArtistId.From(albumId.ArtistId), new CatalogItem.MusicTrack(track)),
            streamingLocations.ToDictionary(
                static location => location.Provider,
                static location => new Uri(location.Url)));
    }
}

internal static class LookupDataCompleteAlbumTrackScenarios
{
    public static AlbumId DefaultAlbumId { get; } =
        AlbumId.From("artist-aurora-lane", "album-midnight-signals");

    public static LookupDataCompleteAlbumTrack MidnightSignals(
        DateTimeOffset catalogUpdatedAt,
        string? spotifyUrl = null) =>
        LookupDataCompleteAlbumTrack.Create(
            DefaultAlbumId,
            "Aurora Lane",
            "Midnight Signals",
            "Midnight Signals",
            new DateOnly(2023, 11, 10),
            null,
            214000,
            catalogUpdatedAt,
            isrc: "GBAYE2301110",
            artworkUrl: "https://cdn.soundtrail.test/tracks/midnight-signals.jpg",
            streamingLocations: spotifyUrl is null
                ? []
                : [(ProviderName.Spotify, spotifyUrl)]);

    public static LookupDataCompleteAlbumTrack StaticHearts(DateTimeOffset catalogUpdatedAt) =>
        LookupDataCompleteAlbumTrack.Create(
            DefaultAlbumId,
            "Aurora Lane",
            "Static Hearts",
            "Midnight Signals",
            new DateOnly(2022, 9, 16),
            null,
            198000,
            catalogUpdatedAt);
}
