using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.Search;

namespace Soundtrail.Services.Enrichment.Shared.MusicTracks;

public sealed record ProviderSnapshot(
    MusicCatalogId MusicCatalogId,
    ProviderName Provider,
    DateTimeOffset CapturedAt,
    string PayloadJson);
