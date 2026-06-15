namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.CatalogSearchTrackingStore;

public static class CatalogSearchTrackingStoreContractModes
{
    public static IEnumerable<object[]> All() =>
    [
        [CatalogSearchTrackingStoreMode.InProcessFake],
        [CatalogSearchTrackingStoreMode.RavenEmbedded]
    ];
}
