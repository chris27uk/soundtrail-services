using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Enrichment.Responses;

public sealed record AlbumMetadataLookupAttempted(
    CommandId CommandId,
    ArtistId ArtistId,
    AlbumId AlbumId,
    LookupSource SourceProvider,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    CorrelationId CorrelationId,
    MusicCatalogLookupOutcome Outcome,
    AlbumMetadataFetched? AlbumMetadataFetched = null) : ICommand
{
    public static AlbumMetadataLookupAttempted Completed(AlbumMetadataFetched fetched) =>
        new(
            fetched.CommandId,
            fetched.ArtistId,
            fetched.AlbumId,
            fetched.SourceProvider,
            fetched.Priority,
            fetched.CreatedAt,
            fetched.CorrelationId,
            MusicCatalogLookupOutcome.Completed(),
            fetched);

    public static AlbumMetadataLookupAttempted Deferred(
        CommandId commandId,
        ArtistId artistId,
        AlbumId albumId,
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
            albumId,
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Deferred(reason, retryAt, retryAfterSeconds));

    public static AlbumMetadataLookupAttempted Duplicate(
        CommandId commandId,
        ArtistId artistId,
        AlbumId albumId,
        LookupSource sourceProvider,
        LookupPriorityBand priority,
        DateTimeOffset createdAt,
        CorrelationId correlationId) =>
        new(
            commandId,
            artistId,
            albumId,
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Duplicate());

    public static AlbumMetadataLookupAttempted Failed(
        CommandId commandId,
        ArtistId artistId,
        AlbumId albumId,
        LookupSource sourceProvider,
        LookupPriorityBand priority,
        DateTimeOffset createdAt,
        CorrelationId correlationId,
        string reason) =>
        new(
            commandId,
            artistId,
            albumId,
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Failed(reason));
}
