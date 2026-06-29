namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.Ports;

public sealed record KnownCatalogAlbumLookupData(
    string ArtistName,
    string AlbumTitle,
    string? MusicBrainzArtistId,
    string? MusicBrainzReleaseId);
