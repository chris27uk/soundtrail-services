using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Catalog.MusicBrainzDumpFreshness;
using Soundtrail.Services.Tests;

namespace Soundtrail.Services.Tests.Unit.Solitary.Worker.MusicBrainzDumpFreshness;

public sealed class MusicBrainzDumpFreshnessPolicyTests
{
    private static readonly DateTimeOffset UtcNow = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan FreshWithin = TimeSpan.FromDays(30);
    private static readonly ArtistId ArtistId = ArtistId.From("artist-aurora");
    private static readonly AlbumId AlbumId = AlbumId.From(ArtistId.Value, "rg-midnight");

    [Fact]
    public void Artist_Albums_Is_Fresh_When_Mb_Id_Window_And_Albums_Present()
    {
        var decision = MusicBrainzDumpFreshnessPolicy.EvaluateArtistAlbums(
            FreshArtist(),
            FreshAlbums(),
            UtcNow,
            FreshWithin);

        decision.UseCatalog.Should().BeTrue();
        decision.CatalogEntries.Should().ContainSingle();
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, false)]
    public void Artist_Albums_Needs_Live_Lookup_When_Any_Gate_Fails(
        bool hasMbId,
        bool withinWindow,
        bool hasAlbums)
    {
        var artist = new DumpCatalogArtistSnapshot(
            ArtistId.Value,
            hasMbId ? "mb-artist-1" : null,
            withinWindow ? UtcNow.AddDays(-1) : UtcNow.AddDays(-40));
        var albums = hasAlbums
            ? FreshAlbums(withinWindow ? UtcNow.AddDays(-1) : UtcNow.AddDays(-40))
            : new DumpCatalogAlbumsSnapshot(ArtistId.Value, UtcNow, []);

        var decision = MusicBrainzDumpFreshnessPolicy.EvaluateArtistAlbums(
            artist,
            albums,
            UtcNow,
            FreshWithin);

        decision.UseCatalog.Should().BeFalse();
    }

    [Fact]
    public void Artist_Tracks_Is_Fresh_When_Mb_Id_Window_And_Tracks_Present()
    {
        var decision = MusicBrainzDumpFreshnessPolicy.EvaluateArtistTracks(
            FreshArtist(),
            [FreshTrack()],
            UtcNow,
            FreshWithin);

        decision.UseCatalog.Should().BeTrue();
        decision.CatalogEntries.Should().ContainSingle();
    }

    [Fact]
    public void Artist_Tracks_Needs_Live_Lookup_When_Tracks_Missing()
    {
        var decision = MusicBrainzDumpFreshnessPolicy.EvaluateArtistTracks(
            FreshArtist(),
            [],
            UtcNow,
            FreshWithin);

        decision.UseCatalog.Should().BeFalse();
    }

    [Fact]
    public void Album_Tracks_Is_Fresh_When_Album_And_Tracks_Present()
    {
        var decision = MusicBrainzDumpFreshnessPolicy.EvaluateAlbumTracks(
            FreshArtist(),
            FreshAlbums(),
            AlbumId,
            [FreshTrack(albumId: AlbumId.StableValue)],
            UtcNow,
            FreshWithin);

        decision.UseCatalog.Should().BeTrue();
        decision.CatalogEntries.Should().ContainSingle();
    }

    [Fact]
    public void Album_Tracks_Matches_By_Album_Title_When_Album_Id_Missing_On_Track()
    {
        var decision = MusicBrainzDumpFreshnessPolicy.EvaluateAlbumTracks(
            FreshArtist(),
            FreshAlbums(),
            AlbumId,
            [FreshTrack(albumId: null, albumTitle: "Midnight Signals")],
            UtcNow,
            FreshWithin);

        decision.UseCatalog.Should().BeTrue();
    }

    [Fact]
    public void Album_Tracks_Needs_Live_Lookup_When_Album_Missing_From_Artist_Albums()
    {
        var decision = MusicBrainzDumpFreshnessPolicy.EvaluateAlbumTracks(
            FreshArtist(),
            FreshAlbums(),
            AlbumId.From(ArtistId.Value, "other-rg"),
            [FreshTrack(albumId: AlbumId.StableValue)],
            UtcNow,
            FreshWithin);

        decision.UseCatalog.Should().BeFalse();
    }

    private static DumpCatalogArtistSnapshot FreshArtist(DateTimeOffset? updatedAt = null) =>
        new(ArtistId.Value, "mb-artist-1", updatedAt ?? UtcNow.AddDays(-2));

    private static DumpCatalogAlbumsSnapshot FreshAlbums(DateTimeOffset? updatedAt = null) =>
        new(
            ArtistId.Value,
            updatedAt ?? UtcNow.AddDays(-2),
            [
                new DumpCatalogAlbumSnapshot(
                    AlbumId.StableValue,
                    "Midnight Signals",
                    new DateOnly(2023, 11, 10),
                    null)
            ]);

    private static DumpCatalogTrackSnapshot FreshTrack(
        string? albumId = null,
        string? albumTitle = "Midnight Signals")
    {
        var trackId = TrackId.TryCreate("Aurora", "Glass Cities", albumTitle, new DateOnly(2023, 11, 10), "album") switch
        {
            TrackIdCreateResult.Success success => success.Value.Value,
            _ => TestTrackIds.Value("glass-cities")
        };

        return new DumpCatalogTrackSnapshot(
            trackId,
            ArtistId.Value,
            "Glass Cities",
            "Aurora",
            albumTitle,
            albumId,
            180_000,
            null,
            new DateOnly(2023, 11, 10),
            "album",
            null,
            UtcNow.AddDays(-2),
            []);
    }
}
