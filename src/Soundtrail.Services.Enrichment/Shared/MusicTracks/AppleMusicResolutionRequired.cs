using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Shared.MusicTracks;

public sealed record AppleMusicResolutionRequired(
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    CorrelationId CorrelationId,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt) : MusicTrackFact(SourceProvider, ObservedAt);
