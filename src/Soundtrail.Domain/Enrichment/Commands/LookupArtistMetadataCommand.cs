using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Enrichment.Commands;

public sealed record LookupArtistMetadataCommand(
    CommandId CommandId,
    ArtistId ArtistId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    CorrelationId CorrelationId,
    string ArtistName,
    string? SourceArtistId = null) : ICommand;
