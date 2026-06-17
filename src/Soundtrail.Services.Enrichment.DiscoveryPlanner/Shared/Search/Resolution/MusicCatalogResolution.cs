using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;

public sealed record MusicCatalogResolution(
    MusicCatalogResolutionOutcome Outcome,
    MusicCatalogId? MusicCatalogId)
{
    public bool IsResolved => Outcome == MusicCatalogResolutionOutcome.Resolved && MusicCatalogId is not null;

    public static MusicCatalogResolution Resolved(MusicCatalogId musicCatalogId) =>
        new(MusicCatalogResolutionOutcome.Resolved, musicCatalogId);

    public static MusicCatalogResolution NotFound() =>
        new(MusicCatalogResolutionOutcome.NotFound, null);

    public static MusicCatalogResolution Ambiguous() =>
        new(MusicCatalogResolutionOutcome.Ambiguous, null);
}
