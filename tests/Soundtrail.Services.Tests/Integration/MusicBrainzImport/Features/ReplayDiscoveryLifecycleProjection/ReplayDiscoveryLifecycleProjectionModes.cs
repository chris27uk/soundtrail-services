namespace Soundtrail.Services.Tests.Integration.MusicBrainzImport.Features.OnReplayCatalogSearchStatus;

public static class ReplayDiscoveryLifecycleProjectionModes
{
    public static IReadOnlyList<object[]> All { get; } =
    [
        [ReplayDiscoveryLifecycleProjectionMode.InProcessFake],
        [ReplayDiscoveryLifecycleProjectionMode.RavenEmbedded]
    ];
}
