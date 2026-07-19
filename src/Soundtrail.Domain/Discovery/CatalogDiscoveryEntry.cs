using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Domain.Discovery;

public sealed record CatalogDiscoveryEntry(ArtistId ArtistId, CatalogItem Item);
