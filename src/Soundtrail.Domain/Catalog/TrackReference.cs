using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Domain.Catalog;

public sealed record TrackReference(ArtistName ArtistName, string TrackTitle);
