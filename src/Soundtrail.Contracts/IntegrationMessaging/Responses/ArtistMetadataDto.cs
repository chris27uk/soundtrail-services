namespace Soundtrail.Contracts.IntegrationMessaging.Responses;

public sealed record ArtistMetadataDto(
    string ArtistName,
    string? SourceArtistId = null);
