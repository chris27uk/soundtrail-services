namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicBrainzMetadataSource;

public static class MusicBrainzMetadataSourceContractModes
{
    public static IEnumerable<object[]> All =>
    [
        [MusicBrainzMetadataSourceMode.InProcessFake],
        [MusicBrainzMetadataSourceMode.HttpAdapter]
    ];
}
