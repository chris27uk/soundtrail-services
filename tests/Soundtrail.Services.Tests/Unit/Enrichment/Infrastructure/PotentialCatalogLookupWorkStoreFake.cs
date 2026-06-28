using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure
{
    internal sealed class PotentialCatalogLookupWorkStoreFake :
        IPotentialCatalogLookupWorkStore,
        ICatalogDiscoveryWorkPlanningReadPort
    {
        private readonly Dictionary<string, PotentialCatalogLookupWork> byMusicCatalogId = [];
        private readonly Dictionary<string, List<IDomainEvent>> eventsByMusicCatalogId = [];

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

        public Task<CatalogDiscoveryWorkEventStream> LoadAsync(
            MusicCatalogId musicCatalogId,
            CancellationToken cancellationToken)
        {
            if (!eventsByMusicCatalogId.TryGetValue(musicCatalogId.Value, out var events))
            {
                events = SeedEventsFromCandidate(musicCatalogId);
            }

            return Task.FromResult(new CatalogDiscoveryWorkEventStream(events.Count, events.ToArray()));
        }

        public Task<bool> AppendAsync(
            MusicCatalogId musicCatalogId,
            int expectedVersion,
            IReadOnlyCollection<IDomainEvent> events,
            CancellationToken cancellationToken)
        {
            if (!eventsByMusicCatalogId.TryGetValue(musicCatalogId.Value, out var storedEvents))
            {
                storedEvents = SeedEventsFromCandidate(musicCatalogId);
            }

            if (storedEvents.Count != expectedVersion)
            {
                return Task.FromResult(false);
            }

            storedEvents.AddRange(events);
            byMusicCatalogId[musicCatalogId.Value] = ApplyToCandidate(
                byMusicCatalogId.TryGetValue(musicCatalogId.Value, out var existing)
                    ? existing
                    : new PotentialCatalogLookupWork(musicCatalogId, 0, 0, 0, CatalogDiscoveryWorkStatus.Pending.ToLegacyStatus(), null),
                events);
            return Task.FromResult(true);
        }

        Task<IReadOnlyList<CatalogDiscoveryWorkSummary>> ICatalogDiscoveryWorkPlanningReadPort.GetPlanningCandidatesAsync(
            DateTimeOffset now,
            int take,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CatalogDiscoveryWorkSummary> candidates = this.byMusicCatalogId.Values
                .Where(candidate => candidate.IsPending && candidate.IsEligibleAt(now))
                .OrderByDescending(candidate => candidate.HighestTrustLevelSeen)
                .ThenByDescending(candidate => candidate.RequestCount)
                .Take(take)
                .Select(candidate => new CatalogDiscoveryWorkSummary(
                    candidate.MusicCatalogId,
                    candidate.RequestCount,
                    candidate.HighestTrustLevelSeen,
                    candidate.RiskScore,
                    candidate.Status.ToDiscoveryStatus(),
                    candidate.NextEligibleAt,
                    Priority: null,
                    Reason: null))
                .ToArray();

            return Task.FromResult(candidates);
        }

        Task<CatalogDiscoveryWorkSummary?> ICatalogDiscoveryWorkPlanningReadPort.LoadAsync(
            MusicCatalogId musicCatalogId,
            CancellationToken cancellationToken)
        {
            if (!byMusicCatalogId.TryGetValue(musicCatalogId.Value, out var candidate))
            {
                return Task.FromResult<CatalogDiscoveryWorkSummary?>(null);
            }

            CatalogDiscoveryWorkSummary summary = new(
                candidate.MusicCatalogId,
                candidate.RequestCount,
                candidate.HighestTrustLevelSeen,
                candidate.RiskScore,
                candidate.Status.ToDiscoveryStatus(),
                candidate.NextEligibleAt,
                Priority: candidate.NextEligibleAt is null && candidate.Status == PotentialCatalogLookupWorkStatus.Pending && candidate.RiskScore < 90
                    ? (candidate.RiskScore >= 30
                        ? LookupPriorityBand.Low
                        : candidate.HighestTrustLevelSeen >= 2 || candidate.RequestCount >= 2
                            ? LookupPriorityBand.High
                        : LookupPriorityBand.Low)
                    : null,
                Reason: candidate.NextEligibleAt is null && candidate.Status == PotentialCatalogLookupWorkStatus.Pending
                    ? "Planner queued lookup"
                    : "Planner deferred lookup");
            return Task.FromResult<CatalogDiscoveryWorkSummary?>(summary);
        }

        public void Seed(CatalogDiscoveryWorkSummary summary)
        {
            byMusicCatalogId[summary.MusicCatalogId.Value] = new PotentialCatalogLookupWork(
                summary.MusicCatalogId,
                summary.RequestCount,
                summary.HighestTrustLevelSeen,
                summary.RiskScore,
                summary.Status.ToLegacyStatus(),
                summary.NextEligibleAt);
        }

        private List<IDomainEvent> SeedEventsFromCandidate(MusicCatalogId musicCatalogId)
        {
            if (!byMusicCatalogId.TryGetValue(musicCatalogId.Value, out var candidate))
            {
                var empty = new List<IDomainEvent>();
                eventsByMusicCatalogId[musicCatalogId.Value] = empty;
                return empty;
            }

            var now = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
            var events = new List<IDomainEvent>();
            for (var i = 0; i < candidate.RequestCount; i++)
            {
                events.Add(new CatalogDiscoveryWorkRequested(
                    candidate.MusicCatalogId,
                    candidate.HighestTrustLevelSeen,
                    candidate.RiskScore,
                    now));
            }

            if (candidate.Status == PotentialCatalogLookupWorkStatus.Ignored || candidate.Status == PotentialCatalogLookupWorkStatus.Resolved)
            {
                events.Add(new CatalogDiscoveryWorkIgnored(
                    candidate.MusicCatalogId,
                    candidate.NextEligibleAt,
                    "Seeded",
                    now));
            }
            else if (candidate.NextEligibleAt is not null)
            {
                events.Add(new CatalogDiscoveryWorkDeferred(
                    candidate.MusicCatalogId,
                    candidate.NextEligibleAt.Value,
                    "Seeded",
                    now));
            }

            eventsByMusicCatalogId[musicCatalogId.Value] = events;
            return events;
        }

        private static PotentialCatalogLookupWork ApplyToCandidate(
            PotentialCatalogLookupWork candidate,
            IReadOnlyCollection<IDomainEvent> events)
        {
            var current = candidate;
            foreach (var @event in events)
            {
                current = @event switch
                {
                    CatalogDiscoveryWorkRequested requested => current with
                    {
                        RequestCount = current.RequestCount + 1,
                        HighestTrustLevelSeen = Math.Max(current.HighestTrustLevelSeen, requested.TrustLevel),
                        RiskScore = Math.Max(current.RiskScore, requested.RiskScore),
                        Status = requested.RiskScore >= 90 ? PotentialCatalogLookupWorkStatus.Ignored : PotentialCatalogLookupWorkStatus.Pending,
                        NextEligibleAt = null
                    },
                    CatalogDiscoveryWorkDeferred deferred => current with
                    {
                        Status = PotentialCatalogLookupWorkStatus.Pending,
                        NextEligibleAt = deferred.NextEligibleAt
                    },
                    CatalogDiscoveryWorkIgnored ignored => current with
                    {
                        Status = PotentialCatalogLookupWorkStatus.Ignored,
                        NextEligibleAt = ignored.NextEligibleAt
                    },
                    CatalogDiscoveryWorkScheduled scheduled => current with
                    {
                        Status = PotentialCatalogLookupWorkStatus.Pending,
                        NextEligibleAt = null
                    },
                    _ => current
                };
            }

            return current;
        }
    }
}
