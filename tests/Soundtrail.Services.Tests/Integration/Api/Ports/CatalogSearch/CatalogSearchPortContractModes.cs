namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogSearch;

public static class CatalogSearchPortContractModes
{
    public static IEnumerable<object[]> All =>
    [
        [CatalogSearchPortMode.InProcessFake],
        [CatalogSearchPortMode.RavenEmbedded]
    ];
}
