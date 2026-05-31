using Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure
{
    internal sealed class RankedMusicCandidateStoreFake : IRankedMusicCandidateStore
    {
        private readonly Dictionary<string, RankedMusicCandidate> byMusicCatalogId = [];

        public IReadOnlyList<RankedMusicCandidate> All => this.byMusicCatalogId.Values.ToArray();

        public Task<RankedMusicCandidate?> FindByMusicCatalogIdAsync(
            MusicCatalogId musicCatalogId,
            CancellationToken cancellationToken)
        {
            this.byMusicCatalogId.TryGetValue(musicCatalogId.Value, out var rankedMusicCandidate);
            return Task.FromResult(rankedMusicCandidate);
        }

        public Task UpsertAsync(
            RankedMusicCandidate candidate,
            CancellationToken cancellationToken)
        {
            this.byMusicCatalogId[candidate.MusicCatalogId.Value] = candidate;
            return Task.CompletedTask;
        }

        public void Seed(RankedMusicCandidate candidate) => this.byMusicCatalogId[candidate.MusicCatalogId.Value] = candidate;
    }
}
