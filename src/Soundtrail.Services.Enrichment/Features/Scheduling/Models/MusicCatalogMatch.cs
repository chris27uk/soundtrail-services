namespace Soundtrail.Services.Enrichment.Features.Scheduling.Models;

public sealed record MusicCatalogMatch(
    MusicCatalogId MusicCatalogId,
    decimal Score);
