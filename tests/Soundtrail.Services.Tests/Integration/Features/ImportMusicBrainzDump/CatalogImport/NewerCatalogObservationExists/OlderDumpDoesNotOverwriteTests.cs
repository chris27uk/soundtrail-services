using Soundtrail.Domain.Catalog.Events;

namespace Soundtrail.Services.Tests.Integration.Features.ImportMusicBrainzDump.CatalogImport.NewerCatalogObservationExists;

public sealed class OlderDumpDoesNotOverwriteTests
{
    [Fact]
    public async Task When_Flushing_Older_Dump_Then_No_Additional_Odesli_Is_Requested()
    {
        await using var environment = CatalogDumpBatchWriterIntegrationTestEnvironment.Create();
        await environment.FlushArtistAlbumAndTrackAsync(DateTimeOffset.Parse("2026-08-10T00:00:00Z"));
        var messagesAfterFreshFlush = environment.CommandBus.SentMessages.Count;

        await environment.FlushArtistAlbumAndTrackAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.CommandBus.SentMessages.Count.Should().Be(messagesAfterFreshFlush);
    }

    [Fact]
    public async Task When_Flushing_Older_Dump_Then_Track_Is_Not_Discovered_Again()
    {
        await using var environment = CatalogDumpBatchWriterIntegrationTestEnvironment.Create();
        await environment.FlushArtistAlbumAndTrackAsync(DateTimeOffset.Parse("2026-08-10T00:00:00Z"));

        await environment.FlushArtistAlbumAndTrackAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.ArtistEvents.SavedEvents.OfType<TrackDiscovered>().Should().ContainSingle();
    }
}
