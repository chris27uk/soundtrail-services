using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling;

public sealed class MusicTrackSearchMatchCollectionTests
{
    [Fact]
    public void Given_An_Exact_Query_Match_When_Querying_Then_It_Is_Selected_Ahead_Of_A_Higher_Scoring_Non_Exact_Match()
    {
        var collection = new MusicTrackSearchMatchCollection(
        [
            Match(
                "mc_track_1",
                0.90m,
                new MusicCatalogMatchEvidence(
                    false,
                    "rare unknown song",
                    "test artist",
                    "rare album",
                    null,
                    null,
                    null)),
            Match(
                "mc_track_2",
                0.95m,
                new MusicCatalogMatchEvidence(
                    false,
                    "rare unknown song",
                    "other artist",
                    "other album",
                    null,
                    null,
                    null))
        ]);

        var actual = collection.Query(MusicSearchCriteria.ByQuery("rare unknown song test artist rare album", SearchTypesFilter.Tracks));

        actual.Select(x => x.MusicCatalogId.Value).Should().Equal("mc_track_1");
    }

    [Fact]
    public void Given_Multiple_Exact_Identity_Matches_And_A_Release_Date_When_Querying_Then_Only_Release_Date_Matches_Are_Selected()
    {
        var collection = new MusicTrackSearchMatchCollection(
        [
            Match(
                "mc_track_1",
                0.99m,
                new MusicCatalogMatchEvidence(true, null, null, null, "usir20400274", "mbid-1", new DateOnly(2004, 6, 7))),
            Match(
                "mc_track_2",
                1.00m,
                new MusicCatalogMatchEvidence(true, null, null, null, "usir20400274", "mbid-2", new DateOnly(2005, 6, 7)))
        ],
            new DateOnly(2004, 6, 7));

        var actual = collection.Query(MusicSearchCriteria.ByTrackArtistAlbum("Rare Unknown Song", "Test Artist", "Rare Album"));

        actual.Select(x => x.MusicCatalogId.Value).Should().Equal("mc_track_1");
    }

    [Fact]
    public void Given_No_Exact_Matches_When_Querying_Then_Only_Matches_Above_The_Minimum_Score_Are_Selected()
    {
        var collection = new MusicTrackSearchMatchCollection(
        [
            Match("mc_track_1", 0.92m),
            Match("mc_track_2", 0.80m),
            Match("mc_track_3", 0.79m)
        ]);

        var actual = collection.Query(MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks));

        actual.Select(x => x.MusicCatalogId.Value).Should().BeEquivalentTo("mc_track_1", "mc_track_2");
    }

    private static MusicCatalogMatch Match(
        string musicCatalogId,
        decimal score,
        MusicCatalogMatchEvidence? evidence = null) =>
        new(MusicCatalogId.From(musicCatalogId), score, evidence ?? MusicCatalogMatchEvidence.None);
}
