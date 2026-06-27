using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Domain.Enrichment.Responses;

public sealed record MusicCatalogLookupAttempted(
    CommandId CommandId,
    MusicCatalogId MusicCatalogId,
    LookupSource SourceProvider,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    CorrelationId CorrelationId,
    MusicCatalogLookupOutcome Outcome,
    MusicCatalogMetadataFetched? MusicCatalogMetadataFetched) : ICommand
{
    public static MusicCatalogLookupAttempted Completed(MusicCatalogMetadataFetched musicCatalogMetadataFetched) =>
        new(
            musicCatalogMetadataFetched.CommandId,
            musicCatalogMetadataFetched.MusicCatalogId,
            musicCatalogMetadataFetched.SourceProvider,
            musicCatalogMetadataFetched.Priority,
            musicCatalogMetadataFetched.CreatedAt,
            musicCatalogMetadataFetched.CorrelationId,
            MusicCatalogLookupOutcome.Completed(),
            musicCatalogMetadataFetched);

    public static MusicCatalogLookupAttempted Deferred(
        CommandId commandId,
        MusicCatalogId musicCatalogId,
        LookupSource sourceProvider,
        LookupPriorityBand priority,
        DateTimeOffset createdAt,
        CorrelationId correlationId,
        string reason,
        DateTimeOffset? retryAt,
        int? retryAfterSeconds) =>
        new(
            commandId,
            musicCatalogId,
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Deferred(reason, retryAt, retryAfterSeconds),
            null);

    public static MusicCatalogLookupAttempted Duplicate(
        CommandId commandId,
        MusicCatalogId musicCatalogId,
        LookupSource sourceProvider,
        LookupPriorityBand priority,
        DateTimeOffset createdAt,
        CorrelationId correlationId) =>
        new(
            commandId,
            musicCatalogId,
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Duplicate(),
            null);

    public static MusicCatalogLookupAttempted Failed(
        CommandId commandId,
        MusicCatalogId musicCatalogId,
        LookupSource sourceProvider,
        LookupPriorityBand priority,
        DateTimeOffset createdAt,
        CorrelationId correlationId,
        string reason) =>
        new(
            commandId,
            musicCatalogId,
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Failed(reason),
            null);
}
