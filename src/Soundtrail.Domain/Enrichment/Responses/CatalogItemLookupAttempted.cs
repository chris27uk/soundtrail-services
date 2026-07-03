using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Enrichment.Responses;

public sealed record CatalogItemLookupAttempted(
    CommandId CommandId,
    CatalogItemId ItemId,
    LookupSource SourceProvider,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    CorrelationId CorrelationId,
    MusicCatalogLookupOutcome Outcome,
    CatalogItemLookupContent? Content = null,
    MusicSearchCriteria? SearchCriteria = null) : ICommand
{
    public MusicCatalogMetadataFetched? MusicCatalogMetadataFetched =>
        Content is CatalogItemLookupContent.Track(var fetched) ? fetched : null;

    public ArtistMetadataFetched? ArtistMetadataFetched =>
        Content is CatalogItemLookupContent.Artist(var fetched) ? fetched : null;

    public AlbumMetadataFetched? AlbumMetadataFetched =>
        Content is CatalogItemLookupContent.Album(var fetched) ? fetched : null;

    public static CatalogItemLookupAttempted Completed(
        MusicCatalogMetadataFetched fetched,
        MusicSearchCriteria? searchCriteria = null) =>
        new(
            fetched.CommandId,
            new CatalogItemId.Track(TrackId.From(fetched.MusicCatalogId.Value)),
            fetched.SourceProvider,
            fetched.Priority,
            fetched.CreatedAt,
            fetched.CorrelationId,
            MusicCatalogLookupOutcome.Completed(),
            new CatalogItemLookupContent.Track(fetched),
            searchCriteria);

    public static CatalogItemLookupAttempted Completed(ArtistMetadataFetched fetched) =>
        new(
            fetched.CommandId,
            new CatalogItemId.Artist(fetched.ArtistId),
            fetched.SourceProvider,
            fetched.Priority,
            fetched.CreatedAt,
            fetched.CorrelationId,
            MusicCatalogLookupOutcome.Completed(),
            new CatalogItemLookupContent.Artist(fetched));

    public static CatalogItemLookupAttempted Completed(AlbumMetadataFetched fetched) =>
        new(
            fetched.CommandId,
            new CatalogItemId.Album(CatalogAlbumId.From(fetched.ArtistId, fetched.AlbumId)),
            fetched.SourceProvider,
            fetched.Priority,
            fetched.CreatedAt,
            fetched.CorrelationId,
            MusicCatalogLookupOutcome.Completed(),
            new CatalogItemLookupContent.Album(fetched));

    public static CatalogItemLookupAttempted Deferred(
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
            new CatalogItemId.Track(TrackId.From(musicCatalogId.Value)),
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Deferred(reason, retryAt, retryAfterSeconds),
            null,
            searchCriteria);

    public static CatalogItemLookupAttempted Deferred(
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
            new CatalogItemId.Artist(artistId),
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Deferred(reason, retryAt, retryAfterSeconds));

    public static CatalogItemLookupAttempted Deferred(
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
            new CatalogItemId.Album(CatalogAlbumId.From(artistId, albumId)),
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Deferred(reason, retryAt, retryAfterSeconds));

    public static CatalogItemLookupAttempted Duplicate(
        CommandId commandId,
        MusicCatalogId musicCatalogId,
        LookupSource sourceProvider,
        LookupPriorityBand priority,
        DateTimeOffset createdAt,
        CorrelationId correlationId,
        MusicSearchCriteria? searchCriteria = null) =>
        new(
            commandId,
            new CatalogItemId.Track(TrackId.From(musicCatalogId.Value)),
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Duplicate(),
            null,
            searchCriteria);

    public static CatalogItemLookupAttempted Duplicate(
        CommandId commandId,
        ArtistId artistId,
        LookupSource sourceProvider,
        LookupPriorityBand priority,
        DateTimeOffset createdAt,
        CorrelationId correlationId) =>
        new(
            commandId,
            new CatalogItemId.Artist(artistId),
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Duplicate());

    public static CatalogItemLookupAttempted Duplicate(
        CommandId commandId,
        ArtistId artistId,
        AlbumId albumId,
        LookupSource sourceProvider,
        LookupPriorityBand priority,
        DateTimeOffset createdAt,
        CorrelationId correlationId) =>
        new(
            commandId,
            new CatalogItemId.Album(CatalogAlbumId.From(artistId, albumId)),
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Duplicate());

    public static CatalogItemLookupAttempted Failed(
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
            new CatalogItemId.Track(TrackId.From(musicCatalogId.Value)),
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Failed(reason),
            null,
            searchCriteria);

    public static CatalogItemLookupAttempted Failed(
        CommandId commandId,
        ArtistId artistId,
        LookupSource sourceProvider,
        LookupPriorityBand priority,
        DateTimeOffset createdAt,
        CorrelationId correlationId,
        string reason) =>
        new(
            commandId,
            new CatalogItemId.Artist(artistId),
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Failed(reason));

    public static CatalogItemLookupAttempted Failed(
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
            new CatalogItemId.Album(CatalogAlbumId.From(artistId, albumId)),
            sourceProvider,
            priority,
            createdAt,
            correlationId,
            MusicCatalogLookupOutcome.Failed(reason));
}
