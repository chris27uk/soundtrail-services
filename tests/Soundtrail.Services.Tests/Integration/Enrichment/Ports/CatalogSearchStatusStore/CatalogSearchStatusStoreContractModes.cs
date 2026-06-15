namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.CatalogSearchStatusStore;

public static class CatalogSearchStatusStoreContractModes
{
    public static IEnumerable<object[]> All =>
    [
        [CatalogSearchStatusStoreMode.InProcessFake],
        [CatalogSearchStatusStoreMode.RavenEmbedded]
    ];
}
