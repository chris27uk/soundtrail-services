using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure
{
    internal sealed class PotentialCatalogLookupWorkStoreFake : IPotentialCatalogLookupWorkStore
    {
        private readonly Dictionary<string, PotentialCatalogLookupWork> byMusicCatalogId = [];

        public IReadOnlyList<PotentialCatalogLookupWork> All => this.byMusicCatalogId.Values.ToArray();

        public Task<PotentialCatalogLookupWork?> FindByMusicCatalogIdAsync(
            MusicCatalogId musicCatalogId,
            CancellationToken cancellationToken)
        {
            this.byMusicCatalogId.TryGetValue(musicCatalogId.Value, out var rankedMusicCandidate);
            return Task.FromResult(rankedMusicCandidate);
        }

        public Task UpsertAsync(
            PotentialCatalogLookupWork candidate,
            CancellationToken cancellationToken)
        {
            this.byMusicCatalogId[candidate.MusicCatalogId.Value] = candidate;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PotentialCatalogLookupWork>> GetPlanningCandidatesAsync(
            DateTimeOffset now,
            int take,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<PotentialCatalogLookupWork> candidates = this.byMusicCatalogId.Values
                .Where(candidate => candidate.IsPending && candidate.IsEligibleAt(now))
                .OrderByDescending(candidate => candidate.HighestTrustLevelSeen)
                .ThenByDescending(candidate => candidate.RequestCount)
                .Take(take)
                .ToArray();

            return Task.FromResult(candidates);
        }

        public void Seed(PotentialCatalogLookupWork candidate) => this.byMusicCatalogId[candidate.MusicCatalogId.Value] = candidate;
    }
}
