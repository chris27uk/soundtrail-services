namespace Soundtrail.Services.Enrichment.Features.Scheduling.Models;

public sealed record ActiveLookupWork(
    MusicCatalogId MusicCatalogId,
    string CommandId,
    DateTimeOffset ReservedUntil);
