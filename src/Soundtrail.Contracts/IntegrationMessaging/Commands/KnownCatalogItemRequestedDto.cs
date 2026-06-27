namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record KnownCatalogItemRequestedDto(
    string? ArtistId,
    string? AlbumId,
    string? TrackId,
    string Playback,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset OccurredAt,
    string CorrelationId);
