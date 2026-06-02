using FluentAssertions;
using Soundtrail.Services.Enrichment.Features.Scheduling;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling;

public class MusicCatalogResolutionPolicyTests
{
    private readonly MusicCatalogResolutionPolicy policy = new();

    [Fact]
    public void Given_No_Matches_When_Resolving_Then_Outcome_Is_NotFound()
    {
        var resolution = this.policy.Resolve([]);

        resolution.Outcome.Should().Be(MusicCatalogResolutionOutcome.NotFound);
        resolution.MusicCatalogId.Should().BeNull();
    }

    [Fact]
    public void Given_A_Strong_Single_Match_When_Resolving_Then_Outcome_Is_Resolved()
    {
        var resolution = this.policy.Resolve([
            new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.95m)
        ]);

        resolution.Outcome.Should().Be(MusicCatalogResolutionOutcome.Resolved);
        resolution.MusicCatalogId?.Value.Should().Be("mc_track_1");
    }

    [Fact]
    public void Given_A_Weak_Top_Match_When_Resolving_Then_Outcome_Is_NotFound()
    {
        var resolution = this.policy.Resolve([
            new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.79m)
        ]);

        resolution.Outcome.Should().Be(MusicCatalogResolutionOutcome.NotFound);
        resolution.MusicCatalogId.Should().BeNull();
    }

    [Fact]
    public void Given_Two_Close_Strong_Matches_When_Resolving_Then_Outcome_Is_Ambiguous()
    {
        var resolution = this.policy.Resolve([
            new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.92m),
            new MusicCatalogMatch(MusicCatalogId.From("mc_track_2"), 0.85m)
        ]);

        resolution.Outcome.Should().Be(MusicCatalogResolutionOutcome.Ambiguous);
        resolution.MusicCatalogId.Should().BeNull();
    }

    [Fact]
    public void Given_A_Clear_Winning_Match_When_Resolving_Then_Outcome_Is_Resolved()
    {
        var resolution = this.policy.Resolve([
            new MusicCatalogMatch(MusicCatalogId.From("mc_track_2"), 0.81m),
            new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.93m)
        ]);

        resolution.Outcome.Should().Be(MusicCatalogResolutionOutcome.Resolved);
        resolution.MusicCatalogId?.Value.Should().Be("mc_track_1");
    }
}
