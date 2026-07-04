using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog.Events;

public sealed record AlbumDiscovered(Album Album, DateTimeOffset ObservedAt) : IMusicTrackEvent;
