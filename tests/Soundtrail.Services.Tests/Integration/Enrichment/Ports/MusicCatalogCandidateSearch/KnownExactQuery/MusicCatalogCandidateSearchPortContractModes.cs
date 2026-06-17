namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicCatalogCandidateSearch.KnownExactQuery
{
    public static class MusicCatalogCandidateSearchPortContractModes
    {
        public static IEnumerable<object[]> All =>
        [
            [MusicCatalogCandidateSearchPortMode.InProcessFake],
            [MusicCatalogCandidateSearchPortMode.RavenEmbedded]
        ];
    }
}