namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogRead;

public static class CatalogReadPortContractModes
{
    public static IEnumerable<object[]> All =>
    [
        [CatalogReadPortMode.InProcessFake],
        [CatalogReadPortMode.RavenEmbedded]
    ];
}
