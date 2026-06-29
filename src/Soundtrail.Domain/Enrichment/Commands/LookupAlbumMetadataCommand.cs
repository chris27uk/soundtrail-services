using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Enrichment.Commands;

public sealed record LookupAlbumMetadataCommand(
    CommandId CommandId,
    ArtistId ArtistId,
    AlbumId AlbumId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    CorrelationId CorrelationId,
    string ArtistName,
    string AlbumTitle,
    string? SourceAlbumId = null,
    string? SourceArtistId = null) : ICommand;
