using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Services.Tests.Integration.Features.ImportMusicBrainzDump.CatalogImport.NoExistingCatalogData;

public sealed class ClearProjectedStreamVersionsAllowsReprojectTests
{
    [Fact]
    public async Task When_Projected_Version_Cleared_Then_Artist_Is_Projected_Again()
    {
        await using var environment = CatalogDumpBatchWriterIntegrationTestEnvironment.Create();
        var observedAt = DateTimeOffset.Parse("2026-08-10T00:00:00Z");
        await environment.FlushArtistAlbumAndTrackAsync(observedAt);

        using (var session = environment.DocumentStore.OpenAsyncSession())
        {
            var artistDoc = await session.LoadAsync<CatalogArtistRecordDto>(
                CatalogArtistRecordDto.GetDocumentId(environment.ArtistId.Value));
            artistDoc.Should().NotBeNull();
            artistDoc!.ProjectedStreamVersion.Should().NotBeNull();

            var tracksDoc = await session.LoadAsync<CatalogArtistTracksRecordDto>(
                CatalogArtistTracksRecordDto.GetDocumentId(environment.ArtistId.Value));
            tracksDoc.Should().NotBeNull();
            // Simulate truncated projection left over from the Raven 25-event page bug.
            tracksDoc!.Tracks = [];
            await session.SaveChangesAsync();
        }

        var cleared = await environment.Subject.ClearProjectedStreamVersionsAsync(CancellationToken.None);
        cleared.Should().BeGreaterThan(0);

        await environment.Subject.ProjectArtistsAsync(
            new HashSet<ArtistId> { environment.ArtistId },
            observedAt,
            CancellationToken.None);

        using var readSession = environment.DocumentStore.OpenAsyncSession();
        var repaired = await readSession.LoadAsync<CatalogArtistTracksRecordDto>(
            CatalogArtistTracksRecordDto.GetDocumentId(environment.ArtistId.Value));
        repaired.Should().NotBeNull();
        repaired!.Tracks.Should().NotBeEmpty();

        var metadata = await readSession.LoadAsync<RavenEventStreamMetadataRecord>(
            $"artist-catalog-stream-streams/{environment.ArtistId.Value}");
        var artistAfter = await readSession.LoadAsync<CatalogArtistRecordDto>(
            CatalogArtistRecordDto.GetDocumentId(environment.ArtistId.Value));
        artistAfter!.ProjectedStreamVersion.Should().Be(metadata!.Version);
    }
}
