using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.Support;
using System.Text.Json;

namespace Soundtrail.Services.Tests.Unit.CatalogProjector.Features.ProjectMusicTrackCatalog;

public sealed class CatalogProjectionMutationServiceTests
{
    private readonly CatalogProjectionMutationService service = new();

    [Fact]
    public void Given_A_Provider_Resolution_Before_Hierarchy_When_Artist_Arrives_Then_Artist_And_Album_Inherit_Provider_State()
    {
        var track = Track();
        track.AvailableProviders = [ProviderName.Spotify.Value];
        track.AlbumId = "album_hot_fuss";
        track.ArtworkUrl = "https://images.example.com/track.png";
        var artist = Artist("artist_the_killers");
        var album = Album("album_hot_fuss");

        service.ApplyStoredEvent(
            StoredEvent(
                "mc_track_1",
                3,
                nameof(ArtistDiscovered),
                new ArtistDiscoveredEventDataRecordDto(
                    "artist_the_killers",
                    "The Killers",
                    ProviderName.MusicBrainz.Value,
                    Clock)),
            new CatalogProjectionDocuments(track, artist, album));

        track.ArtistId.Should().Be("artist_the_killers");
        track.ArtistName.Should().Be("The Killers");
        artist.AvailableProviders.Should().Contain(ProviderName.Spotify.Value);
        artist.ArtworkUrl.Should().Be("https://images.example.com/track.png");
        album.ArtistId.Should().Be("artist_the_killers");
        album.AvailableProviders.Should().Contain(ProviderName.Spotify.Value);
    }

    [Fact]
    public void Given_A_Provider_Failure_When_Applied_Then_Resolved_Providers_And_References_Are_Removed()
    {
        var track = Track();
        track.ArtistId = "artist_the_killers";
        track.AlbumId = "album_hot_fuss";
        track.AvailableProviders = [ProviderName.YoutubeMusic.Value, ProviderName.Spotify.Value];
        track.ProviderReferences =
        [
            new CatalogProviderReferenceRecordDto
            {
                Provider = ProviderName.YoutubeMusic.Value,
                ProviderEntityType = "track",
                ProviderId = "ytm-1",
                Url = "https://music.youtube.com/watch?v=1",
                DiscoveredAt = Clock
            }
        ];
        var artist = Artist("artist_the_killers");
        artist.AvailableProviders = [ProviderName.YoutubeMusic.Value];
        var album = Album("album_hot_fuss");
        album.AvailableProviders = [ProviderName.YoutubeMusic.Value];

        service.ApplyStoredEvent(
            StoredEvent(
                "mc_track_1",
                4,
                nameof(ProviderReferenceLookupFailed),
                new ProviderReferenceLookupFailedEventDataRecordDto(
                    ProviderName.YoutubeMusic.Value,
                    ProviderName.Odesli.Value,
                    Clock)),
            new CatalogProjectionDocuments(track, artist, album));

        track.AvailableProviders.Should().NotContain(ProviderName.YoutubeMusic.Value);
        track.TerminallyUnavailableProviders.Should().Contain(ProviderName.YoutubeMusic.Value);
        track.ProviderReferences.Should().BeEmpty();
        artist.AvailableProviders.Should().NotContain(ProviderName.YoutubeMusic.Value);
        artist.TerminallyUnavailableProviders.Should().Contain(ProviderName.YoutubeMusic.Value);
        album.AvailableProviders.Should().NotContain(ProviderName.YoutubeMusic.Value);
        album.TerminallyUnavailableProviders.Should().Contain(ProviderName.YoutubeMusic.Value);
    }

    [Fact]
    public void Given_A_Metadata_Correction_When_Applied_Then_Track_Artist_And_Album_Are_Repaired()
    {
        var track = Track();
        track.Title = "Mr Brightside";
        track.ArtistName = "Killers";
        track.SearchText = "mr brightside killers";
        var artist = Artist("artist_the_killers");
        var album = Album("album_hot_fuss");

        service.ApplyStoredEvent(
            StoredEvent(
                "mc_track_1",
                2,
                nameof(MetadataCorrected),
                new MetadataCorrectedEventDataRecordDto(
                    "Mr. Brightside",
                    "The Killers",
                    "artist_the_killers",
                    "Hot Fuss",
                    "album_hot_fuss",
                    222000,
                    "USIR20400274",
                    "mbid-1",
                    "admin/repair",
                    Clock)),
            new CatalogProjectionDocuments(track, artist, album));

        track.Title.Should().Be("Mr. Brightside");
        track.ArtistName.Should().Be("The Killers");
        track.ArtistId.Should().Be("artist_the_killers");
        track.AlbumId.Should().Be("album_hot_fuss");
        track.AlbumName.Should().Be("Hot Fuss");
        track.SearchText.Should().Be("mr. brightside the killers");
        artist.Name.Should().Be("The Killers");
        album.Name.Should().Be("Hot Fuss");
        album.ArtistId.Should().Be("artist_the_killers");
    }

    [Fact]
    public void Given_Artist_Artwork_Event_When_Describing_Related_Documents_Then_The_Resolved_Artist_Id_Is_Returned()
    {
        var track = Track();
        track.ArtistId = "artist_fallback";

        var related = service.DescribeRelatedDocuments(
            StoredEvent(
                "mc_track_1",
                5,
                nameof(ArtworkDiscovered),
                new ArtworkDiscoveredEventDataRecordDto(
                    "Artist",
                    "artist_the_killers",
                    "https://images.example.com/artist.png",
                    "worker/musicbrainz",
                    Clock)),
            track);

        related.ArtistId.Should().Be("artist_the_killers");
        related.AlbumId.Should().BeNull();
    }

    [Fact]
    public void Given_Metadata_Correction_With_No_New_Ids_When_Describing_Related_Documents_Then_Existing_Track_Hierarchy_Is_Used()
    {
        var track = Track();
        track.ArtistId = "artist_the_killers";
        track.AlbumId = "album_hot_fuss";

        var related = service.DescribeRelatedDocuments(
            StoredEvent(
                "mc_track_1",
                6,
                nameof(MetadataCorrected),
                new MetadataCorrectedEventDataRecordDto(
                    "Mr. Brightside",
                    "The Killers",
                    null,
                    "Hot Fuss",
                    null,
                    222000,
                    "USIR20400274",
                    "mbid-1",
                    "admin/repair",
                    Clock)),
            track);

        related.ArtistId.Should().Be("artist_the_killers");
        related.AlbumId.Should().Be("album_hot_fuss");
    }

    private static MusicTrackStoredEventRecordDto StoredEvent(string musicCatalogId, int version, string eventType, object data) =>
        new()
        {
            Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId, version),
            MusicCatalogId = musicCatalogId,
            Version = version,
            EventType = eventType,
            Data = JsonSerializer.Serialize(data),
            OccurredAtUtc = Clock
        };

    private static CatalogTrackRecordDto Track() =>
        new()
        {
            Id = CatalogTrackRecordDto.GetDocumentId("mc_track_1"),
            TrackId = "mc_track_1",
            ArtistId = string.Empty,
            AlbumId = string.Empty,
            Title = "Mr. Brightside",
            NormalizedTitle = "mr. brightside",
            ArtistName = "The Killers",
            AlbumName = string.Empty,
            SearchText = "mr. brightside the killers",
            AvailableProviders = [],
            TerminallyUnavailableProviders = [],
            ProviderReferences = []
        };

    private static CatalogArtistRecordDto Artist(string artistId) =>
        new()
        {
            Id = CatalogArtistRecordDto.GetDocumentId(artistId),
            ArtistId = artistId,
            Name = string.Empty,
            NormalizedName = string.Empty,
            AvailableProviders = [],
            TerminallyUnavailableProviders = []
        };

    private static CatalogAlbumRecordDto Album(string albumId) =>
        new()
        {
            Id = CatalogAlbumRecordDto.GetDocumentId(albumId),
            AlbumId = albumId,
            ArtistId = string.Empty,
            Name = string.Empty,
            NormalizedName = string.Empty,
            ArtistName = string.Empty,
            AvailableProviders = [],
            TerminallyUnavailableProviders = []
        };

    private static readonly DateTimeOffset Clock = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);
}
