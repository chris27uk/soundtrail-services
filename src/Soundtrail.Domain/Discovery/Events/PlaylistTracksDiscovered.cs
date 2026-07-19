using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record PlaylistTracksDiscovered(
    PlaylistId PlaylistId,
    TrackId[] Tracks,
    DateTimeOffset ObservedAt) : IDomainEvent;
