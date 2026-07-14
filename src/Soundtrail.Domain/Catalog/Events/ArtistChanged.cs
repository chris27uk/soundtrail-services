using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Domain.Catalog.Events;

public sealed record ArtistChanged(ArtistId ArtistId);
