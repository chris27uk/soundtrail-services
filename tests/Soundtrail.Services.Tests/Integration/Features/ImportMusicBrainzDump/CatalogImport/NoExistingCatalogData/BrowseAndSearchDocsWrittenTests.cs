using Soundtrail.Contracts.Persistence;

namespace Soundtrail.Services.Tests.Integration.Features.ImportMusicBrainzDump.CatalogImport.NoExistingCatalogData;

public sealed class BrowseAndSearchDocsWrittenTests
{
    [Fact]
    public async Task When_Flushing_Then_Artist_Browse_Doc_Is_Written()
    {
        await using var environment = CatalogDumpBatchWriterIntegrationTestEnvironment.Create();

        await environment.FlushArtistAlbumAndTrackAsync();

        using var session = environment.DocumentStore.OpenAsyncSession();
        (await session.LoadAsync<CatalogArtistRecordDto>(
            CatalogArtistRecordDto.GetDocumentId(environment.ArtistId.Value))).Should().NotBeNull();
    }

    [Fact]
    public async Task When_Flushing_Then_Artist_Albums_Browse_Doc_Is_Written()
    {
        await using var environment = CatalogDumpBatchWriterIntegrationTestEnvironment.Create();

        await environment.FlushArtistAlbumAndTrackAsync();

        using var session = environment.DocumentStore.OpenAsyncSession();
        (await session.LoadAsync<CatalogArtistAlbumsRecordDto>(
            CatalogArtistAlbumsRecordDto.GetDocumentId(environment.ArtistId.Value))).Should().NotBeNull();
    }

    [Fact]
    public async Task When_Flushing_Then_Artist_Tracks_Browse_Doc_Is_Written()
    {
        await using var environment = CatalogDumpBatchWriterIntegrationTestEnvironment.Create();

        await environment.FlushArtistAlbumAndTrackAsync();

        using var session = environment.DocumentStore.OpenAsyncSession();
        (await session.LoadAsync<CatalogArtistTracksRecordDto>(
            CatalogArtistTracksRecordDto.GetDocumentId(environment.ArtistId.Value))).Should().NotBeNull();
    }

    [Fact]
    public async Task When_Flushing_Then_Album_Browse_Doc_Is_Written()
    {
        await using var environment = CatalogDumpBatchWriterIntegrationTestEnvironment.Create();

        await environment.FlushArtistAlbumAndTrackAsync();

        using var session = environment.DocumentStore.OpenAsyncSession();
        (await session.LoadAsync<CatalogAlbumRecordDto>(
            CatalogAlbumRecordDto.GetDocumentId(environment.AlbumId.StableValue))).Should().NotBeNull();
    }

    [Fact]
    public async Task When_Flushing_Then_Album_Tracks_Browse_Doc_Is_Written()
    {
        await using var environment = CatalogDumpBatchWriterIntegrationTestEnvironment.Create();

        await environment.FlushArtistAlbumAndTrackAsync();

        using var session = environment.DocumentStore.OpenAsyncSession();
        (await session.LoadAsync<CatalogAlbumTracksRecordDto>(
            CatalogAlbumTracksRecordDto.GetDocumentId(environment.AlbumId.StableValue))).Should().NotBeNull();
    }

    [Fact]
    public async Task When_Flushing_Then_Track_Browse_Doc_Is_Written()
    {
        await using var environment = CatalogDumpBatchWriterIntegrationTestEnvironment.Create();

        await environment.FlushArtistAlbumAndTrackAsync();

        using var session = environment.DocumentStore.OpenAsyncSession();
        (await session.LoadAsync<CatalogTrackRecordDto>(
            CatalogTrackRecordDto.GetDocumentId(environment.TrackId.Value))).Should().NotBeNull();
    }

    [Fact]
    public async Task When_Flushing_Then_Track_Search_Candidate_Is_Written()
    {
        await using var environment = CatalogDumpBatchWriterIntegrationTestEnvironment.Create();

        await environment.FlushArtistAlbumAndTrackAsync();

        using var session = environment.DocumentStore.OpenAsyncSession();
        (await session.LoadAsync<CatalogSearchCandidateRecordDto>(
            CatalogSearchCandidateRecordDto.GetDocumentId(environment.TrackId.Value))).Should().NotBeNull();
    }
}
