using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.EventSourcing;

namespace Soundtrail.Services.Enrichment.Shared.MusicTracks;

public abstract record MusicTrackFact(
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : IDomainEvent;
