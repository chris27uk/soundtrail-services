namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.RankedMusicCandidateStore.UpsertedCandidate
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