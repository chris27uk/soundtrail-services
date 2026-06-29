namespace Soundtrail.Domain.Enrichment.Responses;

public sealed record ArtistMetadata(
    string ArtistName,
    string? SourceArtistId = null);
