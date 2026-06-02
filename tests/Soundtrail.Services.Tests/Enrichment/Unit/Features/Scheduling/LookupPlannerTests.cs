using FluentAssertions;
using Soundtrail.Services.Enrichment.Features.Scheduling;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling;

public class LookupPlannerTests
{
    private readonly LookupPlanner planner = new();
    private static readonly DateTimeOffset Now = new(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Given_A_Pending_Eligible_Low_Risk_Candidate_When_Planned_Then_It_Is_Scheduled_With_Low_Priority()
    {
        var candidate = Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_1"),
            requestCount: 1,
            highestTrustLevelSeen: 1,
            riskScore: 10,
            status: RankedMusicCandidateStatus.Pending,
            nextEligibleAt: null);

        var plan = this.planner.Plan(candidate, Now);

        plan.Disposition.Should().Be(LookupPlanningDisposition.Schedule);
        plan.Priority.Should().Be(LookupPriorityBand.Low);
    }

    [Fact]
    public void Given_A_Pending_Eligible_Medium_Risk_Candidate_When_Planned_Then_It_Is_Scheduled_With_Low_Priority()
    {
        var candidate = Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_1"),
            requestCount: 3,
            highestTrustLevelSeen: 3,
            riskScore: 30,
            status: RankedMusicCandidateStatus.Pending,
            nextEligibleAt: null);

        var plan = this.planner.Plan(candidate, Now);

        plan.Disposition.Should().Be(LookupPlanningDisposition.Schedule);
        plan.Priority.Should().Be(LookupPriorityBand.Low);
    }

    [Fact]
    public void Given_A_High_Trust_Candidate_When_Planned_Then_It_Is_Scheduled_With_High_Priority()
    {
        var candidate = Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_1"),
            requestCount: 1,
            highestTrustLevelSeen: 2,
            riskScore: 10,
            status: RankedMusicCandidateStatus.Pending,
            nextEligibleAt: null);

        var plan = this.planner.Plan(candidate, Now);

        plan.Disposition.Should().Be(LookupPlanningDisposition.Schedule);
        plan.Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public void Given_A_Trust_Level_One_Candidate_When_Planned_Then_It_Is_Scheduled_With_Low_Priority()
    {
        var candidate = Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_1"),
            requestCount: 1,
            highestTrustLevelSeen: 1,
            riskScore: 10,
            status: RankedMusicCandidateStatus.Pending,
            nextEligibleAt: null);

        var plan = this.planner.Plan(candidate, Now);

        plan.Disposition.Should().Be(LookupPlanningDisposition.Schedule);
        plan.Priority.Should().Be(LookupPriorityBand.Low);
    }

    [Fact]
    public void Given_A_Popular_Candidate_When_Planned_Then_It_Is_Scheduled_With_High_Priority()
    {
        var candidate = Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_1"),
            requestCount: 2,
            highestTrustLevelSeen: 1,
            riskScore: 10,
            status: RankedMusicCandidateStatus.Pending,
            nextEligibleAt: null);

        var plan = this.planner.Plan(candidate, Now);

        plan.Disposition.Should().Be(LookupPlanningDisposition.Schedule);
        plan.Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public void Given_A_Request_Count_One_Candidate_When_Planned_Then_It_Is_Scheduled_With_Low_Priority()
    {
        var candidate = Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_1"),
            requestCount: 1,
            highestTrustLevelSeen: 1,
            riskScore: 10,
            status: RankedMusicCandidateStatus.Pending,
            nextEligibleAt: null);

        var plan = this.planner.Plan(candidate, Now);

        plan.Disposition.Should().Be(LookupPlanningDisposition.Schedule);
        plan.Priority.Should().Be(LookupPriorityBand.Low);
    }

    [Fact]
    public void Given_A_High_Risk_Candidate_When_Planned_Then_It_Is_Ignored()
    {
        var candidate = Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_1"),
            requestCount: 2,
            highestTrustLevelSeen: 1,
            riskScore: 60,
            status: RankedMusicCandidateStatus.Pending,
            nextEligibleAt: null);

        var plan = this.planner.Plan(candidate, Now);

        plan.Disposition.Should().Be(LookupPlanningDisposition.Ignore);
        plan.Priority.Should().BeNull();
    }

    [Fact]
    public void Given_A_Blocked_Candidate_When_Planned_Then_It_Is_Ignored()
    {
        var candidate = Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_1"),
            requestCount: 2,
            highestTrustLevelSeen: 1,
            riskScore: 90,
            status: RankedMusicCandidateStatus.Ignored,
            nextEligibleAt: null);

        var plan = this.planner.Plan(candidate, Now);

        plan.Disposition.Should().Be(LookupPlanningDisposition.Ignore);
        plan.Priority.Should().BeNull();
    }

    [Fact]
    public void Given_A_Not_Yet_Eligible_Candidate_When_Planned_Then_It_Is_Deferred()
    {
        var candidate = Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_1"),
            requestCount: 2,
            highestTrustLevelSeen: 1,
            riskScore: 10,
            status: RankedMusicCandidateStatus.Pending,
            nextEligibleAt: Now.AddMinutes(1));

        var plan = this.planner.Plan(candidate, Now);

        plan.Disposition.Should().Be(LookupPlanningDisposition.Defer);
        plan.Priority.Should().BeNull();
    }

    [Fact]
    public void Given_A_Resolved_Candidate_When_Planned_Then_It_Is_Ignored()
    {
        var candidate = Candidates.ExistingCandidate(
            MusicCatalogId.From("mc_track_1"),
            requestCount: 2,
            highestTrustLevelSeen: 1,
            riskScore: 10,
            status: RankedMusicCandidateStatus.Resolved,
            nextEligibleAt: null);

        var plan = this.planner.Plan(candidate, Now);

        plan.Disposition.Should().Be(LookupPlanningDisposition.Ignore);
        plan.Priority.Should().BeNull();
    }
}
