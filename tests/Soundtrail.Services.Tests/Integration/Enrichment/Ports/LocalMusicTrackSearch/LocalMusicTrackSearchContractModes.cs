namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.LocalMusicTrackSearch;

public static class LocalMusicTrackSearchContractModes
{
    public static IEnumerable<object[]> All =>
    [
        [LocalMusicTrackSearchMode.InProcessFake],
        [LocalMusicTrackSearchMode.RavenEmbedded]
    ];
}
