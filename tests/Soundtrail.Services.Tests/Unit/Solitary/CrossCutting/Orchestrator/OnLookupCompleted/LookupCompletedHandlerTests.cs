using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Discovery.Planning;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady.Collaborators;

namespace Soundtrail.Services.Tests.Unit.Solitary.CrossCutting.Orchestrator.OnLookupCompleted;

public sealed class LookupCompletedHandlerTests
{
    [Fact]
    public async Task Given_A_Search_Lookup_Success_With_A_Long_Track_Id_When_Handling_Then_Streaming_Discovery_Command_Id_Is_ServiceBus_Safe()
    {
        var environment = LookupCompletedHandlerUnitTestEnvironment.Create();
        environment.SeedForSearchResult(
            "Midnight Signals Aurora Lane",
            TrackId.From(TestTrackIds.Value("23e97290be26a0d4877206df841e194ede54a324000b461100000000")));
        var subject = environment.CreateSubject();

        await subject.Handle(
            LookupCompletedHandlerUnitTestEnvironment.CreateSearchCompleted(
                "Midnight Signals Aurora Lane",
                TrackId.From(TestTrackIds.Value("23e97290be26a0d4877206df841e194ede54a324000b461100000000"))), TestContext.Current.CancellationToken);

        var command = environment.CommandBus.SentMessages
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<RequestKnownMusicDataMessage>()
            .Subject;
        command.Id.Value.Should().StartWith("RequestKnownMusicData:");
        command.Id.Value.Length.Should().BeLessThanOrEqualTo(128);
    }

    [Fact]
    public async Task Given_A_Deferred_Lookup_Result_When_Handling_Then_Work_Is_Marked_Deferred()
    {
        var environment = LookupCompletedHandlerUnitTestEnvironment.Create();
        var trackId = TestTrackIds.Create("lookup-deferred-1");
        environment.SeedForStreamingLocation(trackId);
        var subject = environment.CreateSubject();

        await subject.Handle(LookupCompletedHandlerUnitTestEnvironment.CreateDeferred(), TestContext.Current.CancellationToken);

        environment.Repository.AppendedEvents.Last().Should().BeOfType<WorkDeferred>();
    }

    [Fact]
    public async Task Given_A_NotFound_Streaming_Attempt_When_Handling_Then_The_Next_Attempt_Is_Dispatched()
    {
        var environment = LookupCompletedHandlerUnitTestEnvironment.Create();
        var trackId = TestTrackIds.Create("lookup-streaming-1");
        environment.SeedForStreamingLocation(trackId);
        var subject = environment.CreateSubject();
        var scheduledAt = new DateTimeOffset(2026, 7, 19, 9, 45, 30, TimeSpan.Zero);
        var target = Work.EnrichTrackStreamingLocation(trackId);
        var dispatch = new DispatchLookupWork(
            target,
            LookupPriorityBand.Low,
            MessageId.Deterministic("DispatchLookupWork", target.NormalisedIdentifier, scheduledAt.ToString("O")),
            CorrelationId.From("corr-streaming-completed"),
            scheduledAt);
        var plan = LookupPlanningPolicy.Build(dispatch);
        var firstAttempt = WorkerCommandFactory.Create(dispatch, plan.Attempts[0]);
        var secondAttempt = WorkerCommandFactory.Create(dispatch, plan.Attempts[1]);

        await subject.Handle(
            LookupCompletedHandlerUnitTestEnvironment.CreateNotFound(
                trackId,
                firstAttempt.Id), TestContext.Current.CancellationToken);

        environment.Repository.AppendedEvents.Should().ContainSingle(e => e is WorkAttemptFailed);
        environment.CommandBus.SentMessages
            .Should()
            .ContainSingle()
            .Which.Id.Should()
            .Be(secondAttempt.Id);
    }

    [Fact]
    public async Task Given_A_Lookup_Result_When_Handling_Then_Stream_Is_Loaded_Once()
    {
        var environment = LookupCompletedHandlerUnitTestEnvironment.Create();
        environment.SeedForStreamingLocation();
        var subject = environment.CreateSubject();

        await subject.Handle(LookupCompletedHandlerUnitTestEnvironment.CreateStreamingLocationCompleted(), TestContext.Current.CancellationToken);

        environment.Repository.LoadCalls.Should().Be(1);
    }

    [Fact]
    public async Task Given_Multiple_Scheduled_Lookups_When_Handling_Then_The_Matching_Scheduled_Work_Is_Completed()
    {
        var environment = LookupCompletedHandlerUnitTestEnvironment.Create();
        var firstTrackId = TestTrackIds.Create("lookup-streaming-first");
        var secondTrackId = TestTrackIds.Create("lookup-streaming-second");
        environment.SeedWithMultipleScheduledStreamingLookups(firstTrackId, secondTrackId);
        var subject = environment.CreateSubject();
        var completed = LookupCompletedHandlerUnitTestEnvironment.CreateStreamingLocationCompleted(
            trackId: firstTrackId,
            originalCommandId: LookupCompletedHandlerUnitTestEnvironment.CreateWorkerCommandIdForScheduledWork(
                Work.EnrichTrackStreamingLocation(firstTrackId),
                new DateTimeOffset(2026, 7, 19, 9, 40, 30, TimeSpan.Zero),
                "streaming-isrc:Spotify"));

        await subject.Handle(completed, TestContext.Current.CancellationToken);

        environment.Repository.AppendedEvents.Last().Should().Be(
            new WorkCompleted(
                Work.EnrichTrackStreamingLocation(firstTrackId),
                LookupPriorityBand.Low,
                "Lookup completed.",
                new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero)));
    }
}
