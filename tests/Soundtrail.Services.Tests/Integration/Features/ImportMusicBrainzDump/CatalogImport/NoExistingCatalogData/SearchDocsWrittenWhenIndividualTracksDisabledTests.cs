using Soundtrail.Contracts.Persistence;

namespace Soundtrail.Services.Tests.Integration.Features.ImportMusicBrainzDump.CatalogImport.NoExistingCatalogData;

public sealed class SearchDocsWrittenWhenIndividualTracksDisabledTests
{
    [Fact]
    public async Task When_Individual_Track_Docs_Disabled_Then_Search_Candidates_Are_Still_Written()
    {
        await using var environment = CatalogDumpBatchWriterIntegrationTestEnvironment.Create(
            writeIndividualTrackAndSearchDocsOnProjection: false);

        await environment.FlushArtistAlbumAndTrackAsync();

        using var session = environment.DocumentStore.OpenAsyncSession();
        (await session.LoadAsync<CatalogTrackRecordDto>(
            CatalogTrackRecordDto.GetDocumentId(environment.TrackId.Value))).Should().BeNull();
        (await session.LoadAsync<CatalogSearchCandidateRecordDto>(
            CatalogSearchCandidateRecordDto.GetDocumentId(environment.TrackId.Value))).Should().NotBeNull();
        (await session.LoadAsync<CatalogSearchCandidateRecordDto>(
            CatalogSearchCandidateRecordDto.GetDocumentId(environment.ArtistId.Value))).Should().NotBeNull();
    }
}
