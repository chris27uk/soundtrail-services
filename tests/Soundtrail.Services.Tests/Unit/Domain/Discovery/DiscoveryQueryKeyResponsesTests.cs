using FluentAssertions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Tests.Unit.Domain.Discovery;

public sealed class DiscoveryQueryKeyResponsesTests
{
    [Fact]
    public void Given_A_Search_Query_When_Building_A_Query_Key_Then_The_Type_And_Query_Are_Preserved()
    {
        var key = DiscoveryQueryKey.Search("track", "karma police");

        key.Value.Should().Be("search:track:karma police");
    }

    [Fact]
    public void Given_An_Artist_Id_When_Building_A_Query_Key_Then_It_Uses_The_Artist_Prefix()
    {
        var key = DiscoveryQueryKey.Artist(ArtistId.From("artist_123"));

        key.Value.Should().Be("artist:artist_123");
    }

    [Fact]
    public void Given_An_Album_Id_When_Building_A_Query_Key_Then_It_Uses_The_Album_Prefix()
    {
        var key = DiscoveryQueryKey.Album(AlbumId.From("album_456"));

        key.Value.Should().Be("album:album_456");
    }

    [Fact]
    public void Given_A_Track_Id_When_Building_A_Query_Key_Then_It_Uses_The_Track_Prefix()
    {
        var key = DiscoveryQueryKey.Track(TrackId.From("track_789"));

        key.Value.Should().Be("track:track_789");
    }
}
