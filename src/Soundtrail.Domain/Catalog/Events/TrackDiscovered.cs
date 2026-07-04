using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog.Events;

public sealed record TrackDiscovered(Track Track, DateTimeOffset ObservedAt) : IMusicTrackEvent;
