using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.EventSourcing;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;

public abstract record MusicTrackFact(
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : IDomainEvent;
