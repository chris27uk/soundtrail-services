using Soundtrail.Services.Enrichment.Features.Scheduling;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling
{
    internal sealed class LookupMusicSchedulerHandlerTestEnvironment
    {
        private readonly FakeMusicCatalogResolutionPort resolutionPort;
        private readonly InMemoryRankedMusicCandidateStore rankedMusicCandidateStore;
        private readonly InMemoryLookupMusicRequestDeadLetterPort deadLetterPort;
        private readonly InMemoryLookupMusicCommandQueue lookupMusicCommandQueue;

        private LookupMusicSchedulerHandlerTestEnvironment(
            FakeMusicCatalogResolutionPort resolutionPort,
            InMemoryRankedMusicCandidateStore rankedMusicCandidateStore,
            InMemoryLookupMusicRequestDeadLetterPort deadLetterPort,
            InMemoryLookupMusicCommandQueue lookupMusicCommandQueue)
        {
            this.resolutionPort = resolutionPort;
            this.rankedMusicCandidateStore = rankedMusicCandidateStore;
            this.deadLetterPort = deadLetterPort;
            this.lookupMusicCommandQueue = lookupMusicCommandQueue;
            Handler = new LookupMusicSchedulerHandler(
                resolutionPort,
                rankedMusicCandidateStore,
                deadLetterPort,
                lookupMusicCommandQueue);
        }

        public LookupMusicSchedulerHandler Handler { get; }

        public FakeMusicCatalogResolutionPort ResolutionPort => this.resolutionPort;

        public IReadOnlyList<RankedMusicCandidate> RankedMusicCandidates => this.rankedMusicCandidateStore.All;

        public IReadOnlyList<DeadLetteredLookupMusicRequest> DeadLetters => this.deadLetterPort.DeadLetters;

        public IReadOnlyList<LookupMusicCommand> Commands => this.lookupMusicCommandQueue.Commands;

        public static LookupMusicSchedulerHandlerTestEnvironment Empty() =>
            new(
                new FakeMusicCatalogResolutionPort(),
                new InMemoryRankedMusicCandidateStore(),
                new InMemoryLookupMusicRequestDeadLetterPort(),
                new InMemoryLookupMusicCommandQueue());

        public static LookupMusicSchedulerHandlerTestEnvironment WithExistingCandidate(RankedMusicCandidate candidate)
        {
            var store = new InMemoryRankedMusicCandidateStore();
            store.Seed(candidate);
            return new LookupMusicSchedulerHandlerTestEnvironment(
                new FakeMusicCatalogResolutionPort(),
                store,
                new InMemoryLookupMusicRequestDeadLetterPort(),
                new InMemoryLookupMusicCommandQueue());
        }

        public LookupMusicRequest Request(
            string query,
            int trustLevel,
            int riskScore,
            DateTimeOffset? occurredAt = null) =>
            new(
                Query: NormalizedSearchQuery.FromText(query),
                TrustLevel: trustLevel,
                RiskScore: riskScore,
                OccurredAt: occurredAt ?? new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero),
                CorrelationId: "correlation-1");
    }
}
