namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.DiscoveryStatusStore;

public static class DiscoveryStatusStoreContractModes
{
    public static IEnumerable<object[]> All =>
    [
        [DiscoveryStatusStoreMode.InProcessFake],
        [DiscoveryStatusStoreMode.RavenEmbedded]
    ];
}
