namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested.Ports;

public sealed record KnownCatalogArtistLookupData(
    string ArtistName,
    string? MusicBrainzArtistId);
