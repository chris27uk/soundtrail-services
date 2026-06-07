using Soundtrail.Services.Enrichment.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Shared.MusicTracks;

public sealed record ProviderPlaybackReferenceResolved(
    ProviderName Provider,
    string? ExternalId,
    Uri Url,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : MusicTrackFact(SourceProvider, ObservedAt);
