using Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure
{
    internal sealed class FakeMusicCatalogCandidateSearch : IMusicCatalogCandidateSearch
    {
        private IReadOnlyList<MusicCatalogMatch> matches = [new(MusicCatalogId.From("mc_default"), 1.00m)];

        public void ResolveAs(MusicCatalogId musicCatalogId) =>
            this.matches = [new MusicCatalogMatch(musicCatalogId, 1.00m)];

        public void ReturnMatches(params MusicCatalogMatch[] matches) => this.matches = matches;

        public void Fails() => this.matches = [];

        public Task<IReadOnlyList<MusicCatalogMatch>> SearchAsync(
            NormalizedSearchQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult(this.matches);
    }
}
