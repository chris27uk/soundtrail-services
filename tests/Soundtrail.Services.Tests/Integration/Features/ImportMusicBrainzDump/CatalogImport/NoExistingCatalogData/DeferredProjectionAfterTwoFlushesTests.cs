using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Tests.Integration.Features.ImportMusicBrainzDump.CatalogImport.NoExistingCatalogData;

public sealed class DeferredProjectionAfterTwoFlushesTests
{
    [Fact]
    public async Task When_Appending_Twice_Then_Projecting_Once_Then_Browse_Docs_Reflect_Final_State()
    {
        await using var environment = CatalogDumpBatchWriterIntegrationTestEnvironment.Create();
        var observedAt = DateTimeOffset.Parse("2026-08-10T00:00:00Z");

        var artist = new Artist
        {
            Id = environment.ArtistId,
            Name = ArtistName.From(environment.DisplayArtistName),
            SourceSystemIds = SourceSystemIdSet.FromLegacyMusicBrainz($"mbid-artist-{environment.ArtistId.Value}")
        };
        var album = new Album(
            environment.AlbumId,
            environment.DisplayAlbumTitle,
            SourceSystemIdSet.FromLegacyMusicBrainz($"mbid-rg-{environment.AlbumId.StableValue}"),
            new DateOnly(2024, 6, 23),
            null,
            observedAt);
        var track = new Track(environment.TrackId)
        {
            Title = environment.DisplayAlbumTitle,
            ArtistName = environment.DisplayArtistName,
            AlbumTitle = environment.DisplayAlbumTitle,
            AlbumId = environment.AlbumId.StableValue,
            UpdatedAt = observedAt
        };
        SourceSystemIdSet.UnionWith(
            track.SourceSystemIds,
            SourceSystemIdSet.FromLegacyMusicBrainz($"mbid-rec-{environment.TrackId.Value}"));

        var touched = new HashSet<ArtistId>();
        foreach (var id in await environment.Subject.AppendEventsAsync(
                     [new ArtistDumpBatchItem(artist), new AlbumDumpBatchItem(album)],
                     observedAt,
                     CancellationToken.None))
        {
            touched.Add(id);
        }

        using (var session = environment.DocumentStore.OpenAsyncSession())
        {
            (await session.LoadAsync<CatalogArtistRecordDto>(
                CatalogArtistRecordDto.GetDocumentId(environment.ArtistId.Value))).Should().BeNull();
        }

        foreach (var id in await environment.Subject.AppendEventsAsync(
                     [new TrackDumpBatchItem(track)],
                     observedAt,
                     CancellationToken.None))
        {
            touched.Add(id);
        }

        await environment.Subject.ProjectArtistsAsync(touched, observedAt, CancellationToken.None);

        using var readSession = environment.DocumentStore.OpenAsyncSession();
        (await readSession.LoadAsync<CatalogArtistRecordDto>(
            CatalogArtistRecordDto.GetDocumentId(environment.ArtistId.Value))).Should().NotBeNull();
        (await readSession.LoadAsync<CatalogAlbumRecordDto>(
            CatalogAlbumRecordDto.GetDocumentId(environment.AlbumId.StableValue))).Should().NotBeNull();
        (await readSession.LoadAsync<CatalogTrackRecordDto>(
            CatalogTrackRecordDto.GetDocumentId(environment.TrackId.Value))).Should().NotBeNull();
        (await readSession.LoadAsync<CatalogSearchCandidateRecordDto>(
            CatalogSearchCandidateRecordDto.GetDocumentId(environment.TrackId.Value))).Should().NotBeNull();
    }
}
