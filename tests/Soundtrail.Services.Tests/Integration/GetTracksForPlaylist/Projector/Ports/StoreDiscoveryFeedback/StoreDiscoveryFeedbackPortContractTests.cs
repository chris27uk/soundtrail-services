using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Tests.Integration.GetTracksForPlaylist.Projector.Ports.StoreDiscoveryFeedback;

public sealed class StoreDiscoveryFeedbackPortContractTests
{
    public static TheoryData<StoreDiscoveryFeedbackPortImplementation> Implementations => new()
    {
        StoreDiscoveryFeedbackPortImplementation.Fake,
        StoreDiscoveryFeedbackPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Playlist_Target_Is_Completed_When_A_Later_Attempt_Fails_Then_Completed_Status_Is_Preserved(
        StoreDiscoveryFeedbackPortImplementation implementation)
    {
        await using var environment = StoreDiscoveryFeedbackPortContractTestEnvironment.Create(implementation);
        var target = environment.PlaylistTarget();

        await environment.Subject.StoreAsync(
            new WorkCompleted(
                target,
                LookupPriorityBand.High,
                "Lookup completed.",
                new DateTimeOffset(2026, 8, 2, 8, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);

        await environment.Subject.StoreAsync(
            new WorkAttemptFailed(
                target,
                "Transient provider failure.",
                new DateTimeOffset(2026, 8, 2, 8, 0, 1, TimeSpan.Zero)),
            CancellationToken.None);

        var record = await environment.LoadAsync(target);

        record.Should().NotBeNull();
        record!.Status.Should().Be("completed");
        record.Reason.Should().Be("Lookup completed.");
    }
}
