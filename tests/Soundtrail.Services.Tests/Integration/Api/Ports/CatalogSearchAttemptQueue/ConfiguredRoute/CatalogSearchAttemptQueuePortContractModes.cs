namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogSearchAttemptQueue.ConfiguredRoute
{
    public static class CatalogSearchAttemptQueuePortContractModes
    {
        public static IEnumerable<object[]> All =>
        [
            [CatalogSearchAttemptQueuePortMode.InMemoryFake],
            [CatalogSearchAttemptQueuePortMode.WolverineLocal]
        ];
    }
}