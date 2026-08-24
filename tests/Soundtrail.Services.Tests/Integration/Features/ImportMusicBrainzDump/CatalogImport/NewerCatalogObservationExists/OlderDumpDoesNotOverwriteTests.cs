using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Artists;

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
    public async Task When_Projecting_The_Same_Dump_Again_Then_No_Additional_Odesli_Is_Requested()
    {
        await using var environment = CatalogDumpBatchWriterIntegrationTestEnvironment.Create();
        var observedAt = DateTimeOffset.Parse("2026-08-10T00:00:00Z");
        await environment.FlushArtistAlbumAndTrackAsync(observedAt);
        var messagesAfterFirstProjection = environment.CommandBus.SentMessages.Count;

        await environment.Subject.ProjectArtistsAsync(
            new HashSet<ArtistId> { environment.ArtistId },
            observedAt,
            CancellationToken.None);

        environment.CommandBus.SentMessages.Count.Should().Be(messagesAfterFirstProjection);
    }

    [Fact]
    public async Task When_Flushing_Older_Dump_Then_Track_Doc_Remains_Available()
    {
        await using var environment = CatalogDumpBatchWriterIntegrationTestEnvironment.Create();
        await environment.FlushArtistAlbumAndTrackAsync(DateTimeOffset.Parse("2026-08-10T00:00:00Z"));

        await environment.FlushArtistAlbumAndTrackAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        using var session = environment.DocumentStore.OpenAsyncSession();
        var track = await session.LoadAsync<CatalogTrackRecordDto>(
            CatalogTrackRecordDto.GetDocumentId(environment.TrackId.Value));

        track.Should().NotBeNull();
    }
}
