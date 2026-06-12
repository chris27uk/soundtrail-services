using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Responses;

public sealed record EnrichmentResponseDto(
    string CommandId,
    string MusicCatalogId,
    string SourceProvider,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    SongMetadataDto? Metadata,
    IReadOnlyList<ExternalReferenceDto> References,
    string CorrelationId);
