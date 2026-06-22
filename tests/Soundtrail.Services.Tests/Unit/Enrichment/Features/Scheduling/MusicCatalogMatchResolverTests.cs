using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search.Resolution;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling;

public sealed class MusicCatalogMatchResolverTests
{
    private readonly MusicCatalogMatchResolver resolver = new();

    [Fact]
    public void Given_An_Exact_Title_Artist_And_Album_Query_Match_When_A_Close_Higher_Scoring_Candidate_Also_Exists_Then_The_Exact_Query_Match_Is_Resolved()
    {
        var result = resolver.Resolve(
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
            ],
            new MusicCatalogResolutionContext("rare unknown song test artist rare album", null));

        result.Should().Be(MusicCatalogResolution.Resolved(MusicCatalogId.From("mc_track_1")));
    }

    [Fact]
    public void Given_Multiple_Exact_Query_Matches_When_Resolving_Then_The_Result_Is_Ambiguous()
    {
        var result = resolver.Resolve(
            [
                Match(
                    "mc_track_1",
                    0.91m,
                    new MusicCatalogMatchEvidence(
                        false,
                        "rare unknown song",
                        "test artist",
                        null,
                        null,
                        null,
                        null)),
                Match(
                    "mc_track_2",
                    0.90m,
                    new MusicCatalogMatchEvidence(
                        false,
                        "rare unknown song",
                        "test artist",
                        null,
                        null,
                        null,
                        null))
            ],
            new MusicCatalogResolutionContext("rare unknown song test artist", null));

        result.Should().Be(MusicCatalogResolution.Ambiguous());
    }

    [Fact]
    public void Given_Multiple_Exact_Identity_Matches_When_Only_One_Matches_The_Release_Date_Then_That_Candidate_Is_Resolved()
    {
        var result = resolver.Resolve(
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
            new MusicCatalogResolutionContext(null, new DateOnly(2004, 6, 7)));

        result.Should().Be(MusicCatalogResolution.Resolved(MusicCatalogId.From("mc_track_1")));
    }

    [Fact]
    public void Given_Multiple_Exact_Identity_Matches_With_The_Same_Release_Date_When_Resolving_Then_The_Result_Is_Ambiguous()
    {
        var result = resolver.Resolve(
            [
                Match(
                    "mc_track_1",
                    0.99m,
                    new MusicCatalogMatchEvidence(true, null, null, null, "usir20400274", "mbid-1", new DateOnly(2004, 6, 7))),
                Match(
                    "mc_track_2",
                    1.00m,
                    new MusicCatalogMatchEvidence(true, null, null, null, "usir20400274", "mbid-2", new DateOnly(2004, 6, 7)))
            ],
            new MusicCatalogResolutionContext(null, new DateOnly(2004, 6, 7)));

        result.Should().Be(MusicCatalogResolution.Ambiguous());
    }

    [Fact]
    public void Given_No_Exact_Identity_When_The_Top_Score_Does_Not_Have_The_Required_Winning_Margin_Then_The_Result_Is_Ambiguous()
    {
        var result = resolver.Resolve(
            [
                Match("mc_track_1", 0.90m),
                Match("mc_track_2", 0.85m)
            ]);

        result.Should().Be(MusicCatalogResolution.Ambiguous());
    }

    private static MusicCatalogMatch Match(
        string musicCatalogId,
        decimal score,
        MusicCatalogMatchEvidence? evidence = null) =>
        new(MusicCatalogId.From(musicCatalogId), score, evidence ?? MusicCatalogMatchEvidence.None);
}
