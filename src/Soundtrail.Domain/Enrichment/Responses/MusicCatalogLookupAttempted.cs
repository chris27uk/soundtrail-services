using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Enrichment.Responses;

public sealed record MusicCatalogLookupAttempted(
    CommandId CommandId,
    MusicCatalogId MusicCatalogId,
    LookupSource SourceProvider,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    CorrelationId CorrelationId,
    MusicCatalogLookupOutcome Outcome,
    MusicCatalogMetadataFetched? MusicCatalogMetadataFetched,
    MusicSearchCriteria? SearchCriteria = null) : ICommand
{
    public static MusicCatalogLookupAttempted Completed(
        MusicCatalogMetadataFetched musicCatalogMetadataFetched,
        MusicSearchCriteria? searchCriteria = null) =>
        new(
            musicCatalogMetadataFetched.CommandId,
            musicCatalogMetadataFetched.MusicCatalogId,
            musicCatalogMetadataFetched.SourceProvider,
            musicCatalogMetadataFetched.Priority,
            musicCatalogMetadataFetched.CreatedAt,
            musicCatalogMetadataFetched.CorrelationId,
            MusicCatalogLookupOutcome.Completed(),
            musicCatalogMetadataFetched,
            searchCriteria);

    public static MusicCatalogLookupAttempted Deferred(
        CommandId commandId,
        MusicCatalogId musicCatalogId,
        LookupSource sourceProvider,
        LookupPriorityBand priority,
        DateTimeOffset createdAt,
        CorrelationId correlationId,
        string reason,
        DateTimeOffset? retryAt,
        int? retryAfterSeconds,
        MusicSearchCriteria? searchCriteria = null) =>
        new(
            commandId,
            musicCatalogId,
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Deferred(reason, retryAt, retryAfterSeconds),
            null,
            searchCriteria);

    public static MusicCatalogLookupAttempted Duplicate(
        CommandId commandId,
        MusicCatalogId musicCatalogId,
        LookupSource sourceProvider,
        LookupPriorityBand priority,
        DateTimeOffset createdAt,
        CorrelationId correlationId,
        MusicSearchCriteria? searchCriteria = null) =>
        new(
            commandId,
            musicCatalogId,
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Duplicate(),
            null,
            searchCriteria);

    public static MusicCatalogLookupAttempted Failed(
        CommandId commandId,
        MusicCatalogId musicCatalogId,
        LookupSource sourceProvider,
        LookupPriorityBand priority,
        DateTimeOffset createdAt,
        CorrelationId correlationId,
        string reason,
        MusicSearchCriteria? searchCriteria = null) =>
        new(
            commandId,
            musicCatalogId,
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Failed(reason),
            null,
            searchCriteria);
}
