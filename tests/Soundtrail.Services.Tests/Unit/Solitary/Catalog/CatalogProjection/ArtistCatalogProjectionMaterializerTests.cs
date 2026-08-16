using Soundtrail.Adapters.CatalogProjection;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.CatalogProjection;

public sealed class ArtistCatalogProjectionMaterializerTests
{
    [Fact]
    public void Given_Artist_Album_And_Track_Events_When_Materializing_Then_Browse_And_Search_Docs_Are_Complete()
    {
        var artistId = ArtistId.From("neon-harbour");
        var albumId = AlbumId.From(artistId.Value, "glass-cities");
        var trackId = TrackId.TryCreate("Neon Harbour", "Glass Cities", "Glass Cities", new DateOnly(2024, 6, 23), "album") switch
        {
            TrackIdCreateResult.Success success => success.Value,
            TrackIdCreateResult.Failure failure => throw new InvalidOperationException(failure.Reason),
            _ => throw new InvalidOperationException("Unexpected TrackId creation result.")
        };
        var observedAt = DateTimeOffset.Parse("2024-06-23T12:00:00Z");
        var artist = new Artist
        {
            Id = artistId,
            Name = ArtistName.From("Neon Harbour"),
            SourceSystemIds = SourceSystemIdSet.FromLegacyMusicBrainz("mbid-neon")
        };
        var album = new Album(albumId, "Glass Cities", SourceSystemIdSet.FromLegacyMusicBrainz("mbid-rg"), new DateOnly(2024, 6, 23), null, observedAt);
        var track = new Track(trackId)
        {
            Title = "Glass Cities",
            ArtistName = "Neon Harbour",
            AlbumTitle = "Glass Cities",
            AlbumId = albumId.StableValue,
            UpdatedAt = observedAt
        };
        SourceSystemIdSet.UnionWith(track.SourceSystemIds, SourceSystemIdSet.FromLegacyMusicBrainz("mbid-rec"));

        var projection = ArtistCatalogProjectionMaterializer.Build(
            artistId,
            [
                new ArtistDiscovered(artist, observedAt),
                new AlbumDiscovered(album, observedAt),
                new TrackDiscovered(track, new CatalogTrackHierarchy(artistId, albumId), observedAt)
            ]);

        projection.MusicBrainzArtistId.Should().Be("mbid-neon");

        var browse = ArtistCatalogProjectionDocuments.CreateBrowseDocuments(projection);
        browse.Select(static pair => pair.Id).Should().Contain([
            CatalogArtistRecordDto.GetDocumentId(artistId.Value),
            CatalogArtistAlbumsRecordDto.GetDocumentId(artistId.Value),
            CatalogArtistTracksRecordDto.GetDocumentId(artistId.Value),
            CatalogAlbumRecordDto.GetDocumentId(albumId.StableValue),
            CatalogAlbumTracksRecordDto.GetDocumentId(albumId.StableValue),
            CatalogTrackRecordDto.GetDocumentId(trackId.Value)
        ]);

        var search = ArtistCatalogProjectionDocuments.CreateSearchCandidateDocuments(
            projection,
            [
                $"artist:{artistId.Value}",
                $"album:{albumId.StableValue}",
                $"track:{trackId.Value}"
            ]);
        search.Should().HaveCount(3);
        search.Select(static pair => pair.Document).Should().AllBeOfType<CatalogSearchCandidateRecordDto>();
    }
}
