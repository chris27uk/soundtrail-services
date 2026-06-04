namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.RankedMusicCandidateStore.UpsertedCandidate
{
    public static class RankedMusicCandidateStorePortContractModes
    {
        public static IEnumerable<object[]> All =>
        [
            [RankedMusicCandidateStorePortMode.InProcessFake],
            [RankedMusicCandidateStorePortMode.RavenEmbedded]
        ];
    }
}