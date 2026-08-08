using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist.Support;

internal sealed record LookupDataCompleteArtistTrack(
    CatalogDiscoveryEntry CatalogEntry,
    IReadOnlyDictionary<ProviderName, Uri> StreamingLocations)
{
    public static LookupDataCompleteArtistTrack Create(
        ArtistId artistId,
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
            DurationMs = durationMs,
            Isrc = isrc,
            ReleaseDate = releaseDate,
            ReleaseType = releaseType,
            ArtworkUrl = artworkUrl,
            UpdatedAt = catalogUpdatedAt
        };

        return new LookupDataCompleteArtistTrack(
            new CatalogDiscoveryEntry(artistId, new CatalogItem.MusicTrack(track)),
            streamingLocations.ToDictionary(
                static location => location.Provider,
                static location => new Uri(location.Url)));
    }
}

internal static class LookupDataCompleteArtistTrackScenarios
{
    public static ArtistId DefaultArtistId { get; } = ArtistId.From("artist-aurora-lane");

    public static LookupDataCompleteArtistTrack MidnightSignals(
        DateTimeOffset catalogUpdatedAt,
        string? spotifyUrl = null) =>
        LookupDataCompleteArtistTrack.Create(
            DefaultArtistId,
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

    public static LookupDataCompleteArtistTrack StaticHearts(DateTimeOffset catalogUpdatedAt) =>
        LookupDataCompleteArtistTrack.Create(
            DefaultArtistId,
            "Aurora Lane",
            "Static Hearts",
            "Static Hearts",
            new DateOnly(2022, 9, 16),
            null,
            198000,
            catalogUpdatedAt);
}
