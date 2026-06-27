using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Catalog.Browsing;

public sealed record GetTrackCommand(
    ArtistId ArtistId,
    AlbumId AlbumId,
    TrackId TrackId,
    PlaybackProviderFilter Playback);
