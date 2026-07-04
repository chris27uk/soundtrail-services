using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog.Events;

public sealed record ArtworkDiscovered(CatalogItemId CatalogItemId, Uri Url, ProviderName Provider, DateTimeOffset ObservedAt) : IMusicTrackEvent;
