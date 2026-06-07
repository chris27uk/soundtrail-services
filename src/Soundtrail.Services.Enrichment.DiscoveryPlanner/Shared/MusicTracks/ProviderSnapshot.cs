using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;

public sealed record ProviderSnapshot(
    MusicCatalogId MusicCatalogId,
    ProviderName Provider,
    DateTimeOffset CapturedAt,
    string PayloadJson);
