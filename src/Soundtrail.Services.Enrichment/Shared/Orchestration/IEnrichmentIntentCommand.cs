using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Shared.Orchestration;

public interface IEnrichmentIntentCommand
{
    CommandId CommandId { get; }

    MusicCatalogId MusicCatalogId { get; }

    LookupPriorityBand Priority { get; }

    DateTimeOffset CreatedAt { get; }

    CorrelationId CorrelationId { get; }

    ProviderName TargetProvider { get; }
}
