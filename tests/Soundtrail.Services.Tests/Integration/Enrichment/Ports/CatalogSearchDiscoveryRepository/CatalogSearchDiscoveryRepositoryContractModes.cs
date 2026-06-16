namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.CatalogSearchDiscoveryRepository;

public static class CatalogSearchDiscoveryRepositoryContractModes
{
    public static IEnumerable<object[]> All =>
    [
        [CatalogSearchDiscoveryRepositoryMode.InProcessFake],
        [CatalogSearchDiscoveryRepositoryMode.RavenEmbedded]
    ];
}
