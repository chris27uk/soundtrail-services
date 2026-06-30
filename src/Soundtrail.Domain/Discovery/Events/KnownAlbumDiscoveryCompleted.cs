using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record KnownAlbumDiscoveryCompleted(
    ArtistId ArtistId,
    AlbumId AlbumId,
    LookupPriorityBand Priority,
    LookupSource SourceProvider,
    string Reason,
    DateTimeOffset CompletedAt,
    string AlbumTitle,
    string ArtistName,
    string? SourceAlbumId,
    string? SourceArtistId,
    DateOnly? ReleaseDate) : IDomainEvent;
