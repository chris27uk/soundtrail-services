using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

public sealed record MusicCatalogMatch(MusicCatalogId MusicCatalogId, decimal Score);
