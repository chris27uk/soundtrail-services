using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

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

        public Task<IReadOnlyList<RankedMusicCandidate>> GetPlanningCandidatesAsync(
            DateTimeOffset now,
            int take,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<RankedMusicCandidate> candidates = this.byMusicCatalogId.Values
                .Where(candidate => candidate.IsPending && candidate.IsEligibleAt(now))
                .OrderByDescending(candidate => candidate.HighestTrustLevelSeen)
                .ThenByDescending(candidate => candidate.RequestCount)
                .Take(take)
                .ToArray();

            return Task.FromResult(candidates);
        }

        public void Seed(RankedMusicCandidate candidate) => this.byMusicCatalogId[candidate.MusicCatalogId.Value] = candidate;
    }
}
