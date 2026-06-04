namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.MusicCatalogCandidateSearch.KnownExactQuery
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