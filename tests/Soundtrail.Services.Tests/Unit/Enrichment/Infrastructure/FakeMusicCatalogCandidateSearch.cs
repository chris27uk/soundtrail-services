using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure
{
    internal sealed class FakeMusicCatalogCandidateSearch : IMusicCatalogCandidateSearch
    {
        private IReadOnlyList<MusicCatalogMatch> matches = [new(MusicCatalogId.From("mc_default"), 1.00m)];

        public static FakeMusicCatalogCandidateSearch CreateResolvingAs(MusicCatalogId musicCatalogId)
        {
            var fake = new FakeMusicCatalogCandidateSearch();
            fake.ResolveAs(musicCatalogId);
            return fake;
        }

        public static FakeMusicCatalogCandidateSearch CreateForAsyncLookupHappyPath() =>
            CreateResolvingAs(MusicCatalogId.From("mc_track_1"));

        public void ResolveAs(MusicCatalogId musicCatalogId) =>
            this.matches =
            [
                new MusicCatalogMatch(
                    musicCatalogId,
                    1.00m,
                    new MusicCatalogMatchEvidence(
                        true,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null))
            ];

        public void ReturnMatches(params MusicCatalogMatch[] matches) => this.matches = matches;

        public void Fails() => this.matches = [];

        public Task<IReadOnlyList<MusicCatalogMatch>> SearchAsync(
            MusicSearchCriteria searchCriteria,
            CancellationToken cancellationToken) =>
            Task.FromResult(this.matches);
    }
}
