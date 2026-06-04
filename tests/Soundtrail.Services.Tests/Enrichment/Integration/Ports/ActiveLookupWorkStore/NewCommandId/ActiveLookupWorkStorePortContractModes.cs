namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.ActiveLookupWorkStore.NewCommandId
{
    public static class ActiveLookupWorkStorePortContractModes
    {
        public static IEnumerable<object[]> All =>
        [
            [ActiveLookupWorkStorePortMode.InProcessFake],
            [ActiveLookupWorkStorePortMode.RavenEmbedded]
        ];
    }
}