using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Tests.Integration.Features.ImportMusicBrainzDump.CatalogImport.NoExistingCatalogData;

/// <summary>
/// Raven LoadStartingWith defaults to 25 docs; projection must page past that so tracks are not dropped.
/// </summary>
public sealed class ProjectsMoreThanTwentyFiveTrackEventsTests
{
    [Fact]
    public async Task When_Artist_Has_More_Than_Twenty_Five_Track_Events_Then_All_Tracks_Are_Projected()
    {
        await using var environment = CatalogDumpBatchWriterIntegrationTestEnvironment.Create();
        var observedAt = DateTimeOffset.Parse("2026-08-10T00:00:00Z");
        const int trackCount = 30;

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

        var items = new List<CatalogDumpBatchItem>
        {
            new ArtistDumpBatchItem(artist),
            new AlbumDumpBatchItem(album)
        };
        var trackIds = new List<TrackId>(trackCount);

        for (var i = 0; i < trackCount; i++)
        {
            var title = $"{environment.DisplayAlbumTitle} Part {i:D2}";
            var trackId = TrackId.TryCreate(
                environment.DisplayArtistName,
                title,
                environment.DisplayAlbumTitle,
                new DateOnly(2024, 6, 23),
                "album") switch
            {
                TrackIdCreateResult.Success success => success.Value,
                TrackIdCreateResult.Failure failure => throw new InvalidOperationException(failure.Reason),
                _ => throw new InvalidOperationException("Unexpected TrackId creation result.")
            };
            trackIds.Add(trackId);
            var track = new Track(trackId)
            {
                Title = title,
                ArtistName = environment.DisplayArtistName,
                AlbumTitle = environment.DisplayAlbumTitle,
                AlbumId = environment.AlbumId.StableValue,
                UpdatedAt = observedAt
            };
            SourceSystemIdSet.UnionWith(
                track.SourceSystemIds,
                SourceSystemIdSet.FromLegacyMusicBrainz($"mbid-rec-{trackId.Value}"));
            items.Add(new TrackDumpBatchItem(track));
        }

        var touched = await environment.Subject.AppendEventsAsync(items, observedAt, CancellationToken.None);
        await environment.Subject.ProjectArtistsAsync(touched, observedAt, CancellationToken.None);

        using var session = environment.DocumentStore.OpenAsyncSession();
        var artistTracks = await session.LoadAsync<CatalogArtistTracksRecordDto>(
            CatalogArtistTracksRecordDto.GetDocumentId(environment.ArtistId.Value));
        artistTracks.Should().NotBeNull();
        artistTracks!.Tracks.Should().HaveCount(trackCount);

        var searchIds = trackIds
            .Select(id => CatalogSearchCandidateRecordDto.GetDocumentId(id.Value))
            .ToArray();
        var searchDocs = await session.LoadAsync<CatalogSearchCandidateRecordDto>(searchIds);
        searchDocs.Values.Count(static doc => doc is not null).Should().Be(trackCount);
        foreach (var doc in searchDocs.Values)
        {
            doc.Should().NotBeNull();
            doc!.SearchText.Should().Be(
                MusicIdentityText.NormalizeFreeText($"{doc.Title} {environment.DisplayArtistName}"));
        }
    }
}
