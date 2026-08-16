using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Tests.Unit.Solitary.CrossCutting.Orchestrator.OnLookupWorkReady;

public sealed class LookupWorkReadyDumpFreshnessSkipTests
{
    [Fact]
    public async Task Artist_Albums_Completes_From_Catalog_And_Skips_MusicBrainz_When_Dump_Fresh()
    {
        var environment = LookupWorkReadyHandlerUnitTestEnvironment.ForDumpFreshArtistAlbums();

        await environment.HandleLookupAsync();

        environment.CommandBus.SentMessages.OfType<LookupMusicbrainzArtistAlbumsMessage>().Should().BeEmpty();
        var completed = environment.CommandBus.SentMessages.OfType<CatalogLookupCompleted>().Should().ContainSingle().Subject;
        var succeeded = completed.Result.Should().BeOfType<LookupResult.Succeeded>().Subject;
        succeeded.Value.Should().BeOfType<LookedUpData.CatalogEntries>().Subject.Values.Should().ContainSingle()
            .Which.Item.Should().BeOfType<CatalogItem.MusicAlbum>();
    }

    [Fact]
    public async Task Artist_Albums_Enqueues_MusicBrainz_When_Dump_Is_Stale()
    {
        var environment = LookupWorkReadyHandlerUnitTestEnvironment.ForDumpStaleArtistAlbumsRequiringLiveLookup();

        await environment.HandleLookupAsync();

        environment.CommandBus.SentMessages.OfType<CatalogLookupCompleted>().Should().BeEmpty();
        environment.CommandBus.SentMessages.OfType<LookupMusicbrainzArtistAlbumsMessage>().Should().ContainSingle();
    }

    [Fact]
    public async Task Artist_Tracks_Completes_From_Catalog_And_Skips_MusicBrainz_When_Dump_Fresh()
    {
        var environment = LookupWorkReadyHandlerUnitTestEnvironment.ForDumpFreshArtistTracks();

        await environment.HandleLookupAsync();

        environment.CommandBus.SentMessages.OfType<LookupMusicbrainzArtistTracksMessage>().Should().BeEmpty();
        environment.CommandBus.SentMessages.OfType<CatalogLookupCompleted>().Should().ContainSingle();
    }

    [Fact]
    public async Task Album_Tracks_Completes_From_Catalog_And_Skips_MusicBrainz_When_Dump_Fresh()
    {
        var environment = LookupWorkReadyHandlerUnitTestEnvironment.ForDumpFreshAlbumTracks();

        await environment.HandleLookupAsync();

        environment.CommandBus.SentMessages.OfType<LookupMusicbrainzAlbumTracksMessage>().Should().BeEmpty();
        environment.CommandBus.SentMessages.OfType<CatalogLookupCompleted>().Should().ContainSingle();
    }
}
