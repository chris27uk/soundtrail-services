using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Domain.Enrichment.Commands;

public sealed record ApplyAlbumMetadataLookupAttemptedToCatalogCommand(
    AlbumMetadataLookupAttempted Attempted) : ICommand
{
    public CommandId CommandId => Attempted.CommandId;

    public CorrelationId CorrelationId => Attempted.CorrelationId;

    public DateTimeOffset CreatedAt => Attempted.CreatedAt;

    public LookupPriorityBand Priority => Attempted.Priority;
}
