namespace Soundtrail.Services.Tests.Integration.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection;

public static class ReplayDiscoveryLifecycleProjectionModes
{
    public static IReadOnlyList<object[]> All { get; } =
    [
        [ReplayDiscoveryLifecycleProjectionMode.InProcessFake],
        [ReplayDiscoveryLifecycleProjectionMode.RavenEmbedded]
    ];
}
