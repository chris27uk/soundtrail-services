using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Tests.Unit.Sociable.Worker.MusicBrainzDumpFreshness;

public sealed class LookupMusicbrainzDumpFreshnessGateTests
{
    [Fact]
    public async Task Artist_Albums_Uses_Catalog_And_Skips_MusicBrainz_When_Dump_Fresh()
    {
        var environment = MusicBrainzDumpFreshnessGateSociableTestEnvironment.ForDumpFreshArtistAlbums();

        await environment.HandleLookupAsync();

        environment.MusicBrainzBrowseCallCount.Should().Be(0);
        environment.SucceededCatalogEntries().Should().ContainSingle()
            .Which.Item.Should().BeOfType<CatalogItem.MusicAlbum>();
    }

    [Fact]
    public async Task Artist_Albums_Calls_MusicBrainz_When_Dump_Is_Stale()
    {
        var environment = MusicBrainzDumpFreshnessGateSociableTestEnvironment
            .ForDumpStaleArtistAlbumsRequiringLiveLookup();

        await environment.HandleLookupAsync();

        environment.MusicBrainzBrowseCallCount.Should().Be(1);
        environment.SucceededCatalogEntries().Should().ContainSingle();
    }

    [Fact]
    public async Task Artist_Tracks_Uses_Catalog_And_Skips_MusicBrainz_When_Dump_Fresh()
    {
        var environment = MusicBrainzDumpFreshnessGateSociableTestEnvironment.ForDumpFreshArtistTracks();

        await environment.HandleLookupAsync();

        environment.MusicBrainzBrowseCallCount.Should().Be(0);
        environment.SucceededCatalogEntries().Should().ContainSingle()
            .Which.Item.Should().BeOfType<CatalogItem.MusicTrack>();
    }

    [Fact]
    public async Task Album_Tracks_Uses_Catalog_And_Skips_MusicBrainz_When_Dump_Fresh()
    {
        var environment = MusicBrainzDumpFreshnessGateSociableTestEnvironment.ForDumpFreshAlbumTracks();

        await environment.HandleLookupAsync();

        environment.MusicBrainzBrowseCallCount.Should().Be(0);
        environment.SucceededCatalogEntries().Should().ContainSingle()
            .Which.Item.Should().BeOfType<CatalogItem.MusicTrack>();
    }
}
