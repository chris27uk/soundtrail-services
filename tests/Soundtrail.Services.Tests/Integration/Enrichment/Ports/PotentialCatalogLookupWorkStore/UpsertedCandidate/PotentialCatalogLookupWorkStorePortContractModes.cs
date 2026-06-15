namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.PotentialCatalogLookupWorkStore.UpsertedCandidate
{
    public static class PotentialCatalogLookupWorkStorePortContractModes
    {
        public static IEnumerable<object[]> All =>
        [
            [PotentialCatalogLookupWorkStorePortMode.InProcessFake],
            [PotentialCatalogLookupWorkStorePortMode.RavenEmbedded]
        ];
    }
}