namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.ActiveLookupWorkStore.NewCommandId
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