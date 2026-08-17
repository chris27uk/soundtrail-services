using FluentAssertions;
using Soundtrail.Services.StreamBrowser;

namespace Soundtrail.Services.Tests.Unit.Solitary.StreamBrowser;

public sealed class StreamKeyBuilderTests
{
    [Fact]
    public void Build_Playlist_From_Name_Uses_Compact_Normalisation()
    {
        var result = StreamKeyBuilder.Build(
            "child_tracks_for_playlist",
            new Dictionary<string, string?> { ["playlistName"] = "Worldwide Song Chart" });

        result.Kind.Should().Be("work");
        result.AggregateType.Should().Be("catalog-stream");
        result.StreamId.Should().Be("child_tracks_for_playlist:worldwidesongchart");
        result.MetadataDocumentId.Should().Be("catalog-stream-streams/child_tracks_for_playlist:worldwidesongchart");
    }

    [Fact]
    public void Build_Artist_Catalog_Uses_Artist_Id()
    {
        var result = StreamKeyBuilder.Build(
            "artist",
            new Dictionary<string, string?> { ["artistId"] = "musicbrainz-artist:nirvana" });

        result.Kind.Should().Be("catalog");
        result.AggregateType.Should().Be("artist-catalog-stream");
        result.StreamId.Should().Be("musicbrainz-artist:nirvana");
    }

    [Fact]
    public void Build_Child_Tracks_For_Album_Joins_Artist_And_Album()
    {
        var result = StreamKeyBuilder.Build(
            "child_tracks_for_album",
            new Dictionary<string, string?>
            {
                ["artistId"] = "musicbrainz-artist:nirvana",
                ["albumId"] = "nevermind"
            });

        result.StreamId.Should().Be("child_tracks_for_album:musicbrainz-artist:nirvana:nevermind");
    }
}
