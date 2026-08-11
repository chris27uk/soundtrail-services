using Soundtrail.Adapters.Projection;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.OnWorkCompleted;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.OnWorkDeferred;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.OnWorkFailed;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.OnWorkIgnored;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.OnWorkRejected;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Solitary.CrossCutting.Projector.OnWorkFeedbackChanged;

internal sealed class WorkFeedbackChangedProjectorUnitTestEnvironment
{
    private WorkFeedbackChangedProjectorUnitTestEnvironment(
        StoreDiscoveryFeedbackPortFake storeDiscoveryFeedbackPort,
        StorePlaylistTracksReadModelPortFake storePlaylistTracksReadModelPort)
    {
        StoreDiscoveryFeedbackPort = storeDiscoveryFeedbackPort;
        StorePlaylistTracksReadModelPort = storePlaylistTracksReadModelPort;
    }

    public StoreDiscoveryFeedbackPortFake StoreDiscoveryFeedbackPort { get; }

    public StorePlaylistTracksReadModelPortFake StorePlaylistTracksReadModelPort { get; }

    public static WorkFeedbackChangedProjectorUnitTestEnvironment Create() =>
        new(new StoreDiscoveryFeedbackPortFake(), StorePlaylistTracksReadModelPortFake.ForRepairTracking());

    public IProjectionEventHandler<WorkCompleted> CreateCompletedHandler() =>
        new WorkCompletedEventHandler(StoreDiscoveryFeedbackPort, StorePlaylistTracksReadModelPort);

    public IProjectionEventHandler<WorkDeferred> CreateDeferredHandler() =>
        new WorkDeferredEventHandler(StoreDiscoveryFeedbackPort);

    public IProjectionEventHandler<WorkRejected> CreateRejectedHandler() =>
        new WorkRejectedEventHandler(StoreDiscoveryFeedbackPort);

    public IProjectionEventHandler<WorkIgnored> CreateIgnoredHandler() =>
        new WorkIgnoredEventHandler(StoreDiscoveryFeedbackPort);

    public IProjectionEventHandler<WorkAttemptFailed> CreateAttemptFailedHandler() =>
        new WorkAttemptFailedEventHandler(StoreDiscoveryFeedbackPort);

    public static WorkCompleted CreateStreamingLookupCompleted(string trackKey = "feedback-track-completed") =>
        new(
            Work.EnrichTrackStreamingLocation(TestTrackIds.Create(trackKey)),
            LookupPriorityBand.High,
            "Lookup completed.",
            new DateTimeOffset(2026, 7, 19, 11, 5, 0, TimeSpan.Zero));

    public static WorkCompleted CreateStreamingLookupExhausted(string trackKey = "feedback-track-exhausted") =>
        new(
            Work.EnrichTrackStreamingLocation(TestTrackIds.Create(trackKey)),
            LookupPriorityBand.High,
            "All lookup attempts exhausted.",
            new DateTimeOffset(2026, 7, 19, 11, 5, 0, TimeSpan.Zero));

    public static WorkCompleted CreatePlaylistLookupCompleted() =>
        new(
            Work.DiscoverPlaylistTracks(PlaylistId.FromPlaylistName("world_top_100")),
            LookupPriorityBand.High,
            "Lookup completed.",
            new DateTimeOffset(2026, 7, 19, 11, 5, 0, TimeSpan.Zero));

    public static WorkDeferred CreateDeferred() =>
        new(
            Work.EnrichTrackStreamingLocation(TestTrackIds.Create("feedback-track-deferred")),
            LookupPriorityBand.Low,
            new DateTimeOffset(2026, 7, 19, 11, 10, 0, TimeSpan.Zero),
            45,
            "Rate limited",
            new DateTimeOffset(2026, 7, 19, 11, 1, 0, TimeSpan.Zero));

    public static WorkRejected CreateRejected() =>
        new(
            Work.EnrichTrackStreamingLocation(TestTrackIds.Create("feedback-track-rejected")),
            LookupPriorityBand.High,
            "Blocked",
            new DateTimeOffset(2026, 7, 19, 11, 2, 30, TimeSpan.Zero));

    public static WorkIgnored CreateIgnored() =>
        new(
            Work.EnrichTrackStreamingLocation(TestTrackIds.Create("feedback-track-ignored")),
            LookupPriorityBand.Low,
            NextEligibleAt: null,
            EstimatedRetryAfterSeconds: null,
            EarliestExpectedCompletionAt: new DateTimeOffset(2026, 7, 19, 11, 4, 0, TimeSpan.Zero),
            Reason: "Already planned",
            IgnoredAt: new DateTimeOffset(2026, 7, 19, 11, 2, 45, TimeSpan.Zero));

    public static WorkAttemptFailed CreateAttemptFailed() =>
        new(
            Work.EnrichTrackStreamingLocation(TestTrackIds.Create("feedback-track-attempt-failed")),
            "Provider timeout",
            new DateTimeOffset(2026, 7, 19, 11, 3, 0, TimeSpan.Zero));
}
