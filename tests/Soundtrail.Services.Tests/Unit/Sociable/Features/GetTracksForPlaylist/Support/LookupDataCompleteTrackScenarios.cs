using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Support;

internal static class LookupDataCompleteTrackScenarios
{
    public static LookupDataCompleteTrack MidnightSignals(
        DateTimeOffset catalogUpdatedAt,
        string? spotifyUrl = null) =>
        LookupDataCompleteTrack.MatchingCatalogTrack(
            "Aurora Lane",
            "Midnight Signals",
            "Aurora Lane",
            "Midnight Signals",
            "Midnight Signals",
            new DateOnly(2023, 11, 10),
            null,
            214000,
            catalogUpdatedAt,
            spotifyUrl is null ? [] : [(ProviderName.Spotify, spotifyUrl)]);

    public static LookupDataCompleteTrack StaticHearts(DateTimeOffset catalogUpdatedAt) =>
        LookupDataCompleteTrack.MatchingCatalogTrack(
            "Paper Tigers", "Static Hearts", "Paper Tigers", "Static Hearts", "Static Hearts",
            new DateOnly(2022, 9, 16), null, 198000, catalogUpdatedAt);

    public static LookupDataCompleteTrack GlassCities(
        DateTimeOffset catalogUpdatedAt,
        string? youtubeMusicUrl = null) =>
        LookupDataCompleteTrack.MatchingCatalogTrack(
            "Neon Harbour",
            "Glass Cities",
            "Neon Harbour",
            "Glass Cities (Radio Edit)",
            "Glass Cities Remixes",
            new DateOnly(2024, 6, 23),
            "Radio Edit",
            231000,
            catalogUpdatedAt,
            youtubeMusicUrl is null ? [] : [(ProviderName.YoutubeMusic, youtubeMusicUrl)]);

    public static LookupDataCompleteTrack GoldenEcho(DateTimeOffset catalogUpdatedAt) =>
        LookupDataCompleteTrack.MatchingCatalogTrack(
            "Saturn Kids", "Golden Echo", "Saturn Kids", "Golden Echo - Radio Edit", "Golden Echo Radio Release",
            new DateOnly(2024, 2, 14), "Radio Edit", 244000, catalogUpdatedAt);
}
