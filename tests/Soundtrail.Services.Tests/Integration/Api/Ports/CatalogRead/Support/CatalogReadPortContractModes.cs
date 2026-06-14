namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogRead.Support;

public static class CatalogReadPortContractModes
{
    public static IEnumerable<object[]> All =>
    [
        [CatalogReadPortMode.InProcessFake],
        [CatalogReadPortMode.RavenEmbedded]
    ];
}
