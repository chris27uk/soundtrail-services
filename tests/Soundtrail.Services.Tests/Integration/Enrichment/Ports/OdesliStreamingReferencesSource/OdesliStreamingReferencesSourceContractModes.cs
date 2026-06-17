namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.OdesliStreamingReferencesSource;

public static class OdesliStreamingReferencesSourceContractModes
{
    public static IEnumerable<object[]> All =>
    [
        [OdesliStreamingReferencesSourceMode.InProcessFake],
        [OdesliStreamingReferencesSourceMode.HttpAdapter]
    ];
}
