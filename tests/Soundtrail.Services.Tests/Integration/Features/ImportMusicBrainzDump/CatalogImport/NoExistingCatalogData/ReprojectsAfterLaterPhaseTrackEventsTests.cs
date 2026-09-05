using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Tests.Integration.Features.ImportMusicBrainzDump.CatalogImport.NoExistingCatalogData;

public sealed class ReprojectsAfterLaterPhaseTrackEventsTests
{
    [Fact]
    public async Task When_Artist_Projected_Then_Tracks_Appended_Then_Project_Again_Then_Track_Browse_Docs_Exist()
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

        var artistTouched = await environment.Subject.AppendEventsAsync(
            [new ArtistDumpBatchItem(artist), new AlbumDumpBatchItem(album)],
            observedAt,
            CancellationToken.None);
        await environment.Subject.ProjectArtistsAsync(artistTouched, observedAt, CancellationToken.None);

        using (var session = environment.DocumentStore.OpenAsyncSession())
        {
            var artistTracks = await session.LoadAsync<CatalogArtistTracksRecordDto>(
                CatalogArtistTracksRecordDto.GetDocumentId(environment.ArtistId.Value));
            artistTracks.Should().NotBeNull();
            artistTracks!.Tracks.Should().BeEmpty();
            (await session.LoadAsync<CatalogTrackRecordDto>(
                CatalogTrackRecordDto.GetDocumentId(environment.TrackId.Value))).Should().BeNull();
        }

        var trackTouched = await environment.Subject.AppendEventsAsync(
            [new TrackDumpBatchItem(track)],
            observedAt,
            CancellationToken.None);
        trackTouched.Should().Contain(environment.ArtistId);

        using (var session = environment.DocumentStore.OpenAsyncSession())
        {
            var metadata = await session.LoadAsync<Soundtrail.Contracts.EventSourcing.RavenEventStreamMetadataRecord>(
                $"artist-catalog-stream-streams/{environment.ArtistId.Value}");
            metadata.Should().NotBeNull();
            metadata!.Version.Should().BeGreaterThan(2);

            var artistDoc = await session.LoadAsync<CatalogArtistRecordDto>(
                CatalogArtistRecordDto.GetDocumentId(environment.ArtistId.Value));
            artistDoc.Should().NotBeNull();
            artistDoc!.ProjectedStreamVersion.Should().NotBeNull();
            artistDoc.ProjectedStreamVersion.Should().BeLessThan(metadata.Version);
        }

        await environment.Subject.ProjectArtistsAsync(trackTouched, observedAt, CancellationToken.None);

        using var readSession = environment.DocumentStore.OpenAsyncSession();
        var projectedTracks = await readSession.LoadAsync<CatalogArtistTracksRecordDto>(
            CatalogArtistTracksRecordDto.GetDocumentId(environment.ArtistId.Value));
        projectedTracks.Should().NotBeNull();
        projectedTracks!.Tracks.Should().ContainSingle(t => t.TrackId == environment.TrackId.Value);

        var trackDoc = await readSession.LoadAsync<CatalogTrackRecordDto>(
            CatalogTrackRecordDto.GetDocumentId(environment.TrackId.Value));
        trackDoc.Should().NotBeNull();
        trackDoc!.Title.Should().Be(environment.DisplayAlbumTitle);

        var artistDocAfter = await readSession.LoadAsync<CatalogArtistRecordDto>(
            CatalogArtistRecordDto.GetDocumentId(environment.ArtistId.Value));
        artistDocAfter.Should().NotBeNull();
        artistDocAfter!.ProjectedStreamVersion.Should().NotBeNull();
        artistDocAfter.ProjectedStreamVersion.Should().BeGreaterThan(0);
    }
}
