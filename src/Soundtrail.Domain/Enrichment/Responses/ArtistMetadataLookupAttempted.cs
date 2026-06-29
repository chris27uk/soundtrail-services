using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Enrichment.Responses;

public sealed record ArtistMetadataLookupAttempted(
    CommandId CommandId,
    ArtistId ArtistId,
    LookupSource SourceProvider,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    CorrelationId CorrelationId,
    MusicCatalogLookupOutcome Outcome,
    ArtistMetadataFetched? ArtistMetadataFetched = null) : ICommand
{
    public static ArtistMetadataLookupAttempted Completed(ArtistMetadataFetched fetched) =>
        new(
            fetched.CommandId,
            fetched.ArtistId,
            fetched.SourceProvider,
            fetched.Priority,
            fetched.CreatedAt,
            fetched.CorrelationId,
            MusicCatalogLookupOutcome.Completed(),
            fetched);

    public static ArtistMetadataLookupAttempted Deferred(
        CommandId commandId,
        ArtistId artistId,
        LookupSource sourceProvider,
        LookupPriorityBand priority,
        DateTimeOffset createdAt,
        CorrelationId correlationId,
        string reason,
        DateTimeOffset? retryAt,
        int? retryAfterSeconds) =>
        new(
            commandId,
            artistId,
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Deferred(reason, retryAt, retryAfterSeconds));

    public static ArtistMetadataLookupAttempted Duplicate(
        CommandId commandId,
        ArtistId artistId,
        LookupSource sourceProvider,
        LookupPriorityBand priority,
        DateTimeOffset createdAt,
        CorrelationId correlationId) =>
        new(
            commandId,
            artistId,
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Duplicate());

    public static ArtistMetadataLookupAttempted Failed(
        CommandId commandId,
        ArtistId artistId,
        LookupSource sourceProvider,
        LookupPriorityBand priority,
        DateTimeOffset createdAt,
        CorrelationId correlationId,
        string reason) =>
        new(
            commandId,
            artistId,
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Failed(reason));
}
