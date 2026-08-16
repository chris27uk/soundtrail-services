using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.MusicBrainzDumpImport;

public sealed class CatalogDumpBatchGroupingTests
{
    [Fact]
    public void Given_Mixed_Items_When_Grouped_By_Artist_Then_Tracks_And_Albums_Share_Artist_Key()
    {
        var artistA = new Artist
        {
            Id = ArtistId.From("artist-a"),
            Name = ArtistName.From("A")
        };
        var album = new Album(
            AlbumId.From("artist-a", "rg-1"),
            "Album",
            [],
            null,
            null,
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"));
        var track = new Track(TrackId.From(TestTrackIds.Value("dump-batch-track-1")))
        {
            AlbumId = AlbumId.From("artist-a", "rg-1").StableValue,
            Title = "Song",
            ArtistName = "A"
        };

        var items = new CatalogDumpBatchItem[]
        {
            new ArtistDumpBatchItem(artistA),
            new AlbumDumpBatchItem(album),
            new TrackDumpBatchItem(track)
        };

        var groups = items
            .GroupBy(static item => item switch
            {
                ArtistDumpBatchItem(var artist) => artist.Id.Value,
                AlbumDumpBatchItem(var a) => a.AlbumId.ArtistId,
                TrackDumpBatchItem(var t) => AlbumId.From(t.AlbumId!).ArtistId,
                _ => throw new InvalidOperationException()
            }, StringComparer.Ordinal)
            .ToArray();

        groups.Should().ContainSingle();
        groups[0].Key.Should().Be("artist-a");
        groups[0].Should().HaveCount(3);
    }
}
